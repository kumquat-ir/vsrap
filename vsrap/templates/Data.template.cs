// This file is generated, do not edit!
// Instead see templates/Data.template.cs, <apworld>/logic/items.lisp, and <apworld>/logic/locations.lisp

using System.Collections.Generic;

namespace vsrap;

public class Data {
    public static Dictionary<long, Decryptor.ID> DECRYPTOR_ITEMS = new() {
    {%- for id, dec in decryptor_items.items() %}
        [{{ id }}] = Decryptor.ID.{{ dec }},
    {%- endfor %}
    };

    public static Dictionary<Decryptor.ID, long> DECRYPTOR_LOCATIONS = new() {
    {%- for dec, id in decryptor_locations.items() %}
        [Decryptor.ID.{{ dec }}] = {{ id }},
    {%- endfor %}
    };

    public static Dictionary<long, int> CARD_ITEMS = new() {
    {%- for id, card in card_items.items() %}
        [{{ id }}] = {{ card }},
    {%- endfor %}
    };

    public static Dictionary<int, long> CARD_LOCATIONS = new() {
    {%- for card, id in card_locations.items() %}
        [{{ card }}] = {{ id }},
    {%- endfor %}
    };

    public static Dictionary<long, float> PHASE_REFILLS = new() {
    {%- for id, amount in phase_amounts.items() %}
        [{{ id }}] = {{ amount }}f,
    {%- endfor %}
    };

    public static Dictionary<(int, int), long> AMBUSH_LOCATIONS = new() {
    {%- for pos, id in ambush_locations.items() %}
        [({{ pos[0] }}, {{ pos[1] }})] = {{ id }},
    {%- endfor %}
    };

    public static Dictionary<PhysicalUpgrade.HealthUpgrade, long> HEALTH_UPGRADE_LOCATIONS = new() {
    {%- for upgrade, id in health_locations.items() %}
        [PhysicalUpgrade.HealthUpgrade.{{ upgrade }}] = {{ id }},
    {%- endfor %}
    };

    public static Dictionary<PhysicalUpgrade.PhaseUpgrade, long> PHASE_UPGRADE_LOCATIONS = new() {
    {%- for upgrade, id in phase_locations.items() %}
        [PhysicalUpgrade.PhaseUpgrade.{{ upgrade }}] = {{ id }},
    {%- endfor %}
    };

    public static Dictionary<PhysicalUpgrade.Orb, long> ORB_LOCATIONS = new() {
    {%- for orb, id in orb_locations.items() %}
        [PhysicalUpgrade.Orb.{{ orb }}] = {{ id }},
    {%- endfor %}
    };
}
