using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;

namespace vsrap;

public class APSession {
    public static ArchipelagoSession session = null;
    public static APSaveData currentSave = null;
    public static Dictionary<long, ScoutedItemInfo> uncollectedLocationInfo = null;
    public static Dictionary<long, int> sessionRecievedMultiples = new();

    public static void connect() {
        if (SaveDataPatches.loadError != null) {
            // something errored earlier in save loading so we shouldn't continue
            return;
        }

        if (session != null) {
            VSRAP.logger.LogWarning("Cleaning up existing AP session before setting up a new one.");
            cleanupSession();
        }

        VSRAP.logger.LogInfo($"Connecting to {currentSave.connection.address} port {currentSave.connection.port} as {currentSave.connection.slot}");
        Notifications.clearQueue(); // there may be some error notifs piled up from earlier failed connections

        session = ArchipelagoSessionFactory.CreateSession(currentSave.connection.address, currentSave.connection.port);
        session.Socket.ErrorReceived += logReceivedError;
        session.Items.ItemReceived += itemReceived;
        session.Locations.CheckedLocationsUpdated += locationsChecked;

        LoginResult loginResult;
        try {
            VSRAP.logger.LogInfo("Logging in...");
            loginResult = session.TryConnectAndLogin("Vision Soft Reset",
                                                     currentSave.connection.slot,
                                                     ItemsHandlingFlags.AllItems,
                                                     password: currentSave.connection.password,
                                                     tags: ["NoText"]);
        }
        catch (Exception e) {
            loginResult = new LoginFailure(e.GetBaseException().Message);
        }

        if (!loginResult.Successful) {
            LoginFailure failure = (LoginFailure)loginResult;
            string error = "Error connecting:";
            string userError = "Unknown connection error";
            foreach (string err in failure.Errors) {
                error += $"\n  {err}";
                userError = err;
            }
            foreach (ConnectionRefusedError err in failure.ErrorCodes) {
                error += $"\n  {err}";
                userError = err.ToString();
            }
            VSRAP.logger.LogError(error);
            SaveDataPatches.loadError = userError;
            cleanupSession();
            return;
        }

        VSRAP.logger.LogInfo("Connected!");

        Notifications.queueNotification($"Connected to {currentSave.connection.address}!");

        IEnumerable<long> locationsNotOnServer =
            currentSave.checkedLocations.Where(id =>
                                               session.Locations.AllLocations.Contains(id) &&
                                               session.Locations.AllMissingLocations.Contains(id));
        if (locationsNotOnServer.Any()) {
            Notifications.queueNotification($"Sending {locationsNotOnServer.Count()} missed location(s)...");
            session.Locations.CompleteLocationChecks(locationsNotOnServer.ToArray());
        }

        getUncollectedLocationInfo();
    }

    private async static void getUncollectedLocationInfo() {
        uncollectedLocationInfo = await APSession.session.Locations.ScoutLocationsAsync(HintCreationPolicy.None, session.Locations.AllMissingLocations.ToArray());
    }

    public static void cleanupSession() {
        if (session == null) {
            return;
        }

        uncollectedLocationInfo = null;
        sessionRecievedMultiples.Clear();
        session.Socket.DisconnectAsync();
        session.Socket.ErrorReceived -= logReceivedError;
        session.Items.ItemReceived -= itemReceived;
        session.Locations.CheckedLocationsUpdated -= locationsChecked;
        session = null;
    }

    private static HashSet<string> knownErrors = new();
    public static void logReceivedError(Exception e, string message) {
        if (!knownErrors.Contains(message)) {
            VSRAP.logger.LogError(e);
            VSRAP.logger.LogError(message);
            Notifications.queueNotification("Recieved an error: you may be disconnected!");
            knownErrors.Add(message);
        }
    }

    public static void itemReceived(IReceivedItemsHelper helper) {
        ItemInfo item = helper.PeekItem();
        if (currentSave.receivedItems.Contains(item.ItemId)) {
            if (currentSave.recievedMultiples.ContainsKey(item.ItemId)) {
                if (!sessionRecievedMultiples.ContainsKey(item.ItemId)) {
                    sessionRecievedMultiples[item.ItemId] = 0;
                }
                sessionRecievedMultiples[item.ItemId]++;

                if (currentSave.recievedMultiples[item.ItemId] >= sessionRecievedMultiples[item.ItemId]) {
                    helper.DequeueItem();
                    return;
                }
            }
            else {
                helper.DequeueItem();
                return;
            }
        }

        VSRAP.logger.LogInfo($"Received {item.ItemDisplayName} from {item.Player.Alias} ({item.LocationDisplayName})");
        bool selfItem = item.Player.Slot == session.ConnectionInfo.Slot;
        CheckHandler.getItem(item.ItemId);
        if (selfItem) {
            Notifications.queueNotification($"Found your {item.ItemDisplayName} at {item.LocationDisplayName}");
        }
        else {
            Notifications.queueNotification($"Received {item.ItemDisplayName} from {item.Player.Alias} ({item.LocationDisplayName})");
        }
        helper.DequeueItem();
    }

    public static void locationsChecked(ReadOnlyCollection<long> newLocations) {
        VSRAP.logger.LogInfo($"{newLocations.Count} location(s) collected: {String.Join(" ", newLocations)}");
        foreach (long id in newLocations) {
            CheckHandler.externalCollectLocation(id);
        }
    }
}
