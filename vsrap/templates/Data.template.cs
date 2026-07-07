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
}
