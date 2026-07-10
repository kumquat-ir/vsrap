using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace vsrap;

public class ConnectionInfoDialog : MonoBehaviour {
    private enum Selection {
        ADDRESS,
        PORT,
        SLOT,
        PASSWORD,
        CONNECT,
        BACK,
        MAX
    }

    private static bool shown = false;
    private static Selection? showAt = null;
    private static List<GlyphBox> staticText = new();
    private static List<GlyphBox> inputText = new();
    private static GlyphBox currentInputText = null;
    private static Selection selection;

    private static void select(Selection newSelection, MonoBehaviour selector) {
        newSelection = (Selection) Mathf.Clamp((int)newSelection, 0, (int)Selection.MAX - 1);
        selector.enabled = true;
        selector.GetComponent<RectTransform>().localPosition = new Vector2(0, 175 - 75 * (int)newSelection);

        if (newSelection == Selection.CONNECT || newSelection == Selection.BACK) {
            currentInputText = null;
        }
        else {
            currentInputText = inputText[(int) newSelection];
        }

        selection = newSelection;
    }

    private static GlyphBox createGlyphBox(string text, string gameObjectName, FileSelectScreen parent, GameObject dialog, int width = 0, int height = 0) {
        // begin the magical incantation of "make GlyphBox work without inspector-set fields"
        if (width == 0) {
            width = text.Length + 1; // the text -> lines function is slightly buggy if the text has no spaces and length == width, so we need to +1 here
        }
        if (height == 0) {
            height = Mathf.Min(text.Split('\n').Length, 1);
        }
        ConnectionInfoPatches.nextGlyphBoxSize = (width, height);
        GameObject glyphBoxObject = new GameObject(gameObjectName, typeof(RectTransform));
        glyphBoxObject.layer = parent.gameObject.layer;
        glyphBoxObject.AddComponent<GlyphBox>();
        glyphBoxObject.transform.SetParent(dialog.transform, false);
        glyphBoxObject.transform.localScale = new Vector3(2f, 2f, 1f);
        GlyphBox glyphBox = glyphBoxObject.GetComponent<GlyphBox>();
        glyphBox.alignment = GlyphBox.Alignment.CENTER;
        glyphBox.setColor(PauseScreen.DEFAULT_COLOR);
        glyphBox.setPlainText(text);
        glyphBox.makeAllCharsInvisible();
        return glyphBox;
    }

    public static void layoutDialog(FileSelectScreen parent) {
        staticText.Clear();

        GameObject dialog = new GameObject("APConnectionDialog", typeof(RectTransform), typeof(ConnectionInfoDialog));
        dialog.transform.SetParent(parent.transform, false);
        float basex = parent.centerPos.x - 100f;
        float basey = parent.centerPos.y;
        float spacing = -75f;
        float vindex = -1f;

        GlyphBox headerText = createGlyphBox("Connection Info", "ConnectionHeader", parent, dialog);
        headerText.GetComponent<RectTransform>().localPosition = new Vector2(basex, basey + spacing * vindex);
        staticText.Add(headerText);
        vindex++;

        GlyphBox addressText = createGlyphBox("Address:", "AddressText", parent, dialog);
        addressText.GetComponent<RectTransform>().localPosition = new Vector2(basex, basey + spacing * vindex);
        staticText.Add(addressText);
        GlyphBox addressInput = createGlyphBox("", "AddressInput", parent, dialog, 50, 1);
        addressInput.GetComponent<RectTransform>().localPosition = new Vector2(basex, basey + spacing * vindex - 25);
        inputText.Add(addressInput);
        vindex++;

        GlyphBox portText = createGlyphBox("Port:", "PortText", parent, dialog);
        portText.GetComponent<RectTransform>().localPosition = new Vector2(basex, basey + spacing * vindex);
        staticText.Add(portText);
        GlyphBox portInput = createGlyphBox("", "PortInput", parent, dialog, 50, 1);
        portInput.GetComponent<RectTransform>().localPosition = new Vector2(basex, basey + spacing * vindex - 25);
        inputText.Add(portInput);
        vindex++;

        GlyphBox slotText = createGlyphBox("Slot:", "SlotText", parent, dialog);
        slotText.GetComponent<RectTransform>().localPosition = new Vector2(basex, basey + spacing * vindex);
        staticText.Add(slotText);
        GlyphBox slotInput = createGlyphBox("", "SlotInput", parent, dialog, 50, 1);
        slotInput.GetComponent<RectTransform>().localPosition = new Vector2(basex, basey + spacing * vindex - 25);
        inputText.Add(slotInput);
        vindex++;

        GlyphBox passwordText = createGlyphBox("Password:", "PasswordText", parent, dialog);
        passwordText.GetComponent<RectTransform>().localPosition = new Vector2(basex, basey + spacing * vindex);
        staticText.Add(passwordText);
        GlyphBox passwordInput = createGlyphBox("", "PasswordInput", parent, dialog, 50, 1);
        passwordInput.GetComponent<RectTransform>().localPosition = new Vector2(basex, basey + spacing * vindex - 25);
        passwordInput.insertSecretText(new string('*', 49), 0, 0);
        inputText.Add(passwordInput);
        vindex++;

        GlyphBox connectText = createGlyphBox("Connect", "ConnectText", parent, dialog);
        connectText.GetComponent<RectTransform>().localPosition = new Vector2(basex, basey + spacing * vindex - 12.5f);
        staticText.Add(connectText);
        vindex++;

        GlyphBox backText = createGlyphBox("Back", "BackText", parent, dialog);
        backText.GetComponent<RectTransform>().localPosition = new Vector2(basex, basey + spacing * vindex - 12.5f);
        staticText.Add(backText);
        vindex++;

        shown = false;
    }

    private void OnGUI() {
        if (shown && currentInputText != null && Event.current.type == EventType.KeyDown) {
            if (Event.current.character >= ' ') {
                currentInputText.setPlainText(currentInputText.formattedText + Event.current.character);
            }
            else if (Event.current.keyCode == KeyCode.Backspace && currentInputText.formattedText.Length > 0) {
                currentInputText.setPlainText(currentInputText.formattedText.Substring(0, currentInputText.formattedText.Length - 1));
            }

            if (selection == Selection.PASSWORD) {
                currentInputText.visibleChars = 0;
            }
        }
    }

    public static void destroy() {
        foreach (GlyphBox text in staticText) {
            Object.Destroy(text.gameObject);
        }
        staticText.Clear();
        foreach (GlyphBox text in inputText) {
            Object.Destroy(text.gameObject);
        }
        inputText.Clear();
        currentInputText = null;
    }

    public static void selectNewFile(FileSelectScreen screen, int index, Vars.Difficulty _difficulty, bool _tutorials) {
        Vars.saveFileIndexLastUsed = index;
        Vars.loadData(index);
        showAt = Selection.ADDRESS;
        show(APSession.currentSave.connection);
    }

    public static void selectExistingFile(FileSelectScreen screen, int index) {
        Vars.saveFileIndexLastUsed = index;
        if (!Vars.loadData(index)) {
            screen.showError($"Failed to load FILE {index}");
            return;
        }
        else if (SaveDataPatches.handleLoadError(screen)) {
            showAt = Selection.CONNECT;
            show(APSession.currentSave.connection);
        }
    }

    public static void show(APConnectionData connectionInfo) {
        inputText[(int) Selection.ADDRESS].setPlainText(connectionInfo.address);
        inputText[(int) Selection.PORT].setPlainText(connectionInfo.port.ToString());
        inputText[(int) Selection.SLOT].setPlainText(connectionInfo.slot);
        inputText[(int) Selection.PASSWORD].setPlainText(connectionInfo.password);
        staticText[0].setPlainText("Connection Info");
    }

    public static void tryConnect(FileSelectScreen screen) {
        APSession.currentSave.connection.address = inputText[(int) Selection.ADDRESS].formattedText;
        APSession.currentSave.connection.port = int.Parse(inputText[(int) Selection.PORT].formattedText);
        APSession.currentSave.connection.slot = inputText[(int) Selection.SLOT].formattedText;
        APSession.currentSave.connection.password = inputText[(int) Selection.PASSWORD].formattedText;
        staticText[0].setPlainText("Connecting...");

        APSession.connect();
        if (SaveDataPatches.handleLoadError(screen)) {
            staticText[0].setPlainText("Connected!");
            ConnectionInfoPatches.whiteScreenTransition(screen);
        }
        else {
            staticText[0].setPlainText("Connection Error");
        }
    }

    private static void showDialog(List<FileSelect> fileSelects, FileSelectDeleteButton parentDeleteButton, FileSelectBackButton parentBackButton) {
        foreach (GlyphBox text in staticText) {
            text.makeAllCharsVisible();
        }
        foreach (GlyphBox text in inputText) {
            text.makeAllCharsVisible();
        }
        inputText[(int) Selection.PASSWORD].visibleChars = 0;
        inputText[(int) Selection.PASSWORD].visibleSecretChars = 50;

        foreach (FileSelect fileSelect in fileSelects) {
            // GlyphBox does not like being deactivated, so avoid doing that by sending the file selects to narnia, just like vanilla does
            fileSelect.GetComponent<RectTransform>().localPosition = new Vector2(fileSelect.GetComponent<RectTransform>().localPosition.x, fileSelect.GetComponent<RectTransform>().localPosition.y - 1000f);
        }
        parentBackButton.gameObject.SetActive(false);
        parentDeleteButton.gameObject.SetActive(false);

        shown = true;
    }

    private static void hideDialog(List<FileSelect> fileSelects, FileSelectDeleteButton parentDeleteButton, FileSelectBackButton parentBackButton, FileSelectScreen parent) {
        foreach (GlyphBox text in staticText) {
            text.makeAllCharsInvisible();
        }
        foreach (GlyphBox text in inputText) {
            text.makeAllCharsInvisible();
            text.visibleSecretChars = 0;
        }

        foreach (FileSelect fileSelect in fileSelects) {
            fileSelect.GetComponent<RectTransform>().localPosition = new Vector2(fileSelect.GetComponent<RectTransform>().localPosition.x, fileSelect.GetComponent<RectTransform>().localPosition.y + 1000f);
        }
        parentBackButton.gameObject.SetActive(true);
        parentDeleteButton.gameObject.SetActive(true);

        shown = false;

        int temp = parent.selectionIndex;
        parent.selectionIndex = temp - 1;
        parent.selectionIndex = temp;
    }

    public static bool updateDialog(FileSelectScreen parent,
                                    List<FileSelect> fileSelects,
                                    FileSelectDeleteButton parentDeleteButton,
                                    FileSelectBackButton parentBackButton,
                                    MonoBehaviour selector) {

        if (shown) {
            if (Keys.instance.escapePressed) {
                hideDialog(fileSelects, parentDeleteButton, parentBackButton, parent);
            }
            else if (selection == Selection.CONNECT) {
                if (Keys.instance.confirmPressed || Keys.instance.startPressed) {
                    tryConnect(parent);
                }
                else if (Keys.instance.backPressed) {
                    hideDialog(fileSelects, parentDeleteButton, parentBackButton, parent);
                }
            }
            else if (selection == Selection.BACK) {
                if (Keys.instance.confirmPressed || Keys.instance.startPressed || Keys.instance.backPressed) {
                    hideDialog(fileSelects, parentDeleteButton, parentBackButton, parent);
                }
            }

            if (Keys.instance.downPressed) {
                select(selection + 1, selector);
            }
            else if (Keys.instance.upPressed) {
                select(selection - 1, selector);
            }

            return true;
        }
        else if (showAt.HasValue) {
            select(showAt.Value, selector);
            showDialog(fileSelects, parentDeleteButton, parentBackButton);
            showAt = null;
        }

        return false;
    }
}

[HarmonyPatch]
public class ConnectionInfoPatches {
    private static int glyphsID;
    public static (int, int)? nextGlyphBoxSize = null;

    [HarmonyPatch(typeof(FileSelectScreen), "Update")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> addUpdateHandler(IEnumerable<CodeInstruction> insns, ILGenerator generator) {
        Label connectionInfoBegin = generator.DefineLabel();
        Label settingsBegin = generator.DefineLabel();

        return new CodeMatcher(insns)
            .MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(FileSelectScreen), "settingsShown")))
            .Advance(-1)
            .AddLabels([settingsBegin])
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FileSelectScreen), "fileSelects")),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FileSelectScreen), "deleteButton")),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FileSelectScreen), "backButton")),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FileSelectScreen), "selection")),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ConnectionInfoDialog), nameof(ConnectionInfoDialog.updateDialog))),
                new CodeInstruction(OpCodes.Brfalse_S, settingsBegin),
                new CodeInstruction(OpCodes.Ret)
            ).AddLabels([connectionInfoBegin])
            .MatchBack(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(FileSelectScreen), "get_errorShown")))
            .Advance(1)
            .SetOperandAndAdvance(connectionInfoBegin)
            .Start()
            .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(FileSelectScreen), "beginNewFile")))
            .Repeat(match => match.SetOperandAndAdvance(AccessTools.Method(typeof(ConnectionInfoDialog), nameof(ConnectionInfoDialog.selectNewFile))))
            .Start()
            .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(FileSelectScreen), "beginFile")))
            .SetOperandAndAdvance(AccessTools.Method(typeof(ConnectionInfoDialog), nameof(ConnectionInfoDialog.selectExistingFile)))
            .InstructionEnumeration();
    }

    [HarmonyPatch(typeof(FileSelectScreen), "Awake")]
    [HarmonyPostfix]
    static void awake(FileSelectScreen __instance) {
        glyphsID = GameObject.Find("GlyphBox").GetInstanceID();
        ConnectionInfoDialog.layoutDialog(__instance);
    }

    [HarmonyPatch(typeof(FileSelectScreen), "OnDestroy")]
    [HarmonyPostfix]
    static void destroy() {
        ConnectionInfoDialog.destroy();
    }

    [HarmonyPatch(typeof(GlyphBox), "Awake")]
    [HarmonyPrefix]
    static void ensureGlyphs(GlyphBox __instance) {
        if (__instance.glyphGameObjects == null) {
            __instance.glyphGameObjects = ((GameObject) Resources.InstanceIDToObject(glyphsID)).GetComponent<GlyphBox>().glyphGameObjects;
        }
        if (__instance.defaultStyle == null) {
            __instance.defaultStyle = new();
        }
        if (nextGlyphBoxSize.HasValue) {
            __instance.width = nextGlyphBoxSize.Value.Item1;
            __instance.height = nextGlyphBoxSize.Value.Item2;
            nextGlyphBoxSize = null;
        }
    }

    [HarmonyPatch(typeof(FileSelectScreen), "whiteScreenTransition")]
    [HarmonyReversePatch]
    public static void whiteScreenTransition(FileSelectScreen instance) {
        throw new System.NotImplementedException("stub method called??");
    }
}
