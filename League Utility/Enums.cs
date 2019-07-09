﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAccepter
{
    public class Enums
    {
        public static String GetChampionById(int id)
        {
            switch (id)
            {
                case 266: return "Aatrox";
                case 103: return "Ahri";
                case 84: return "Akali";
                case 12: return "Alistar";
                case 32: return "Amumu";
                case 34: return "Anivia";
                case 1: return "Annie";
                case 22: return "Ashe";
                case 136: return "Aurelion Sol";
                case 268: return "Azir";
                case 432: return "Bard";
                case 53: return "Blitzcrank";
                case 63: return "Brand";
                case 201: return "Braum";
                case 51: return "Caitlyn";
                case 164: return "Camille";
                case 69: return "Cassiopeia";
                case 31: return "Cho'Gath";
                case 42: return "Corki";
                case 122: return "Darius";
                case 131: return "Diana";
                case 36: return "Dr. Mundo";
                case 119: return "Draven";
                case 245: return "Ekko";
                case 60: return "Elise";
                case 28: return "Evelynn";
                case 81: return "Ezreal";
                case 9: return "Fiddlesticks";
                case 114: return "Fiora";
                case 105: return "Fizz";
                case 3: return "Galio";
                case 41: return "Gangplank";
                case 86: return "Garen";
                case 150: return "Gnar";
                case 79: return "Gragas";
                case 104: return "Graves";
                case 120: return "Hecarim";
                case 74: return "Heimerdinger";
                case 420: return "Illaoi";
                case 39: return "Irelia";
                case 427: return "Ivern";
                case 40: return "Janna";
                case 59: return "Jarvan IV";
                case 24: return "Jax";
                case 126: return "Jayce";
                case 202: return "Jhin";
                case 222: return "Jinx";
                case 145: return "Kai'Sa";
                case 429: return "Kalista";
                case 43: return "Karma";
                case 30: return "Karthus";
                case 38: return "Kassadin";
                case 55: return "Katarina";
                case 10: return "Kayle";
                case 141: return "Kayn";
                case 85: return "Kennen";
                case 121: return "Kha'Zix";
                case 203: return "Kindred";
                case 240: return "Kled";
                case 96: return "Kog'Maw";
                case 7: return "LeBlanc";
                case 64: return "Lee Sin";
                case 89: return "Leona";
                case 127: return "Lissandra";
                case 236: return "Lucian";
                case 117: return "Lulu";
                case 99: return "Lux";
                case 54: return "Malphite";
                case 90: return "Malzahar";
                case 57: return "Maokai";
                case 11: return "Master Yi";
                case 21: return "Miss Fortune";
                case 82: return "Mordekaiser";
                case 25: return "Morgana";
                case 267: return "Nami";
                case 75: return "Nasus";
                case 111: return "Nautilus";
                case 76: return "Nidalee";
                case 56: return "Nocturne";
                case 20: return "Nunu & Willump";
                case 2: return "Olaf";
                case 61: return "Orianna";
                case 516: return "Ornn";
                case 80: return "Pantheon";
                case 78: return "Poppy";
                case 555: return "Pyke";
                case 133: return "Quinn";
                case 497: return "Rakan";
                case 33: return "Rammus";
                case 421: return "Rek'Sai";
                case 58: return "Renekton";
                case 107: return "Rengar";
                case 92: return "Riven";
                case 68: return "Rumble";
                case 13: return "Ryze";
                case 113: return "Sejuani";
                case 35: return "Shaco";
                case 98: return "Shen";
                case 102: return "Shyvana";
                case 27: return "Singed";
                case 14: return "Sion";
                case 15: return "Sivir";
                case 72: return "Skarner";
                case 37: return "Sona";
                case 16: return "Soraka";
                case 50: return "Swain";
                case 134: return "Syndra";
                case 223: return "Tahm Kench";
                case 163: return "Taliyah";
                case 91: return "Talon";
                case 44: return "Taric";
                case 17: return "Teemo";
                case 412: return "Thresh";
                case 18: return "Tristana";
                case 48: return "Trundle";
                case 23: return "Tryndamere";
                case 4: return "Twisted Fate";
                case 29: return "Twitch";
                case 77: return "Udyr";
                case 6: return "Urgot";
                case 110: return "Varus";
                case 67: return "Vayne";
                case 45: return "Veigar";
                case 161: return "Vel'Koz";
                case 254: return "Vi";
                case 112: return "Viktor";
                case 8: return "Vladimir";
                case 106: return "Volibear";
                case 19: return "Warwick";
                case 62: return "Wukong";
                case 498: return "Xayah";
                case 101: return "Xerath";
                case 5: return "Xin Zhao";
                case 157: return "Yasuo";
                case 83: return "Yorick";
                case 154: return "Zac";
                case 238: return "Zed";
                case 115: return "Ziggs";
                case 26: return "Zilean";
                case 142: return "Zoe";
                case 143: return "Zyra";
                default:
                    return "None";
            }
        }

        public static int GetChampionByName(string name)
        {
            switch (name)
            {
                case "Aatrox": return 266;
                case "Ahri": return 103;
                case "Akali": return 84;
                case "Alistar": return 12;
                case "Amumu": return 32;
                case "Anivia": return 34;
                case "Annie": return 1;
                case "Ashe": return 22;
                case "Aurelion Sol": return 136;
                case "Azir": return 268;
                case "Bard": return 432;
                case "Blitzcrank": return 53;
                case "Brand": return 63;
                case "Braum": return 201;
                case "Caitlyn": return 51;
                case "Camille": return 164;
                case "Cassiopeia": return 69;
                case "Cho'Gath": return 31;
                case "Corki": return 42;
                case "Darius": return 122;
                case "Diana": return 131;
                case "Dr. Mundo": return 36;
                case "Draven": return 119;
                case "Ekko": return 245;
                case "Elise": return 60;
                case "Evelynn": return 28;
                case "Ezreal": return 81;
                case "Fiddlesticks": return 9;
                case "Fiora": return 114;
                case "Fizz": return 105;
                case "Galio": return 3;
                case "Gangplank": return 41;
                case "Garen": return 86;
                case "Gnar": return 150;
                case "Gragas": return 79;
                case "Graves": return 104;
                case "Hecarim": return 120;
                case "Heimerdinger": return 74;
                case "Illaoi": return 420;
                case "Irelia": return 39;
                case "Ivern": return 427;
                case "Janna": return 40;
                case "Jarvan IV": return 59;
                case "Jax": return 24;
                case "Jayce": return 126;
                case "Jhin": return 202;
                case "Jinx": return 222;
                case "Kai'Sa": return 145;
                case "Kalista": return 429;
                case "Karma": return 43;
                case "Karthus": return 30;
                case "Kassadin": return 38;
                case "Katarina": return 55;
                case "Kayle": return 10;
                case "Kayn": return 141;
                case "Kennen": return 85;
                case "Kha'Zix": return 121;
                case "Kindred": return 203;
                case "Kled": return 240;
                case "Kog'Maw": return 96;
                case "LeBlanc": return 7;
                case "Lee Sin": return 64;
                case "Leona": return 89;
                case "Lissandra": return 127;
                case "Lucian": return 236;
                case "Lulu": return 117;
                case "Lux": return 99;
                case "Malphite": return 54;
                case "Malzahar": return 90;
                case "Maokai": return 57;
                case "Master Yi": return 11;
                case "Miss Fortune": return 21;
                case "Mordekaiser":  return 82;
                case "Morgana": return 25;
                case "Nami": return 267;
                case "Nasus": return 75;
                case "Nautilus": return 111;
                case "Nidalee": return 76;
                case "Nocturne": return 56;
                case "Nunu & Willump": return 20;
                case "Olaf": return 2;
                case "Orianna": return 61;
                case "Ornn": return 516;
                case "Pantheon": return 80;
                case "Poppy": return 78;
                case "Pyke": return 555;
                case "Quinn": return 133;
                case "Rakan": return 497;
                case "Rammus": return 33;
                case "Rek'Sai": return 421;
                case "Renekton": return 58;
                case "Rengar": return 107;
                case "Riven": return 92;
                case "Rumble": return 68;
                case "Ryze": return 13;
                case "Sejuani": return 113;
                case "Shaco": return 35;
                case "Shen": return 98;
                case "Shyvana": return 102;
                case "Singed": return 27;
                case "Sion": return 14;
                case "Sivir": return 15;
                case "Skarner": return 72;
                case "Sona": return 37;
                case "Soraka": return 16;
                case "Swain": return 50;
                case "Syndra": return 134;
                case "Tahm Kench": return 223;
                case "Taliyah": return 163;
                case "Talon": return 91;
                case "Taric": return 44;
                case "Teemo": return 17;
                case "Thresh": return 412;
                case "Tristana": return 18;
                case "Trundle": return 48;
                case "Tryndamere": return 23;
                case "Twisted Fate": return 4;
                case "Twitch": return 29;
                case "Udyr": return 77;
                case "Urgot": return 6;
                case "Varus": return 110;
                case "Vayne": return 67;
                case "Veigar": return 45;
                case "Vel'Koz": return 161;
                case "Vi": return 254;
                case "Viktor": return 112;
                case "Vladimir": return 8;
                case "Volibear": return 106;
                case "Warwick": return 19;
                case "Wukong": return 62;
                case "Xayah": return 498;
                case "Xerath": return 101;
                case "Xin Zhao": return 5;
                case "Yasuo": return 157;
                case "Yorick": return 83;
                case "Zac": return 154;
                case "Zed": return 238;
                case "Ziggs": return 115;
                case "Zilean": return 26;
                case "Zoe": return 142;
                case "Zyra": return 143;
                default:
                    return 0;
            }
        }
    }
}
