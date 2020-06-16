using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using InfinityScript;
using static InfinityScript.GSCFunctions;

//*************************************************************
//TODO:
//XX-Visual effects in the world for the circle
//XX-More accurate representation of the circle in the minimap
//XX-'Grace period' from circle damage during first spawn
//XX-A few more minor situational bugs with storm accuracy(End-circle world to map size, removing circle when map size becomes 0)
//XX-Stop storm counter/movement on endGame
//XX-Remove game hud on endGame
//XX-Fix an occurance where health can surpass 125 taking fall damage with shield
//XX-Fix all weapon damage alt addresses
//XX-Finish Damage Dealt hud placements
//-Potential fixes for radar: 
//*******
//compassObjectiveMaxRange 0
//compassObjectiveMinAlpha 0
//(Causes icon to fade on both radars)
//*******
//Set map icon size to 0 on each size update
//compassMaxRange 0.0001
//compassRadarPingFadeTime 0
//*******
//
//*************************************************************

public class br : BaseScript
{
    private static short fx_carePackageImpact;
    public static short fx_smallFire;
    public static short fx_glowStickGlow;
    public static short fx_crateCollectSmoke;
    private static short fx_airdropThruster;
    public static short fx_glow_grey;
    public static short fx_glow_green;
    public static short fx_glow_blue;
    public static short fx_glow_purple;
    public static short fx_glow_gold;
    private static short fx_circleWall;
    private const int icon_optic = 56;
    private const int icon_misc_attach = 59;
    private static bool gameHasStarted = false;
    private static bool rankingStarted = false;
    private static bool firstCircle = true;
    private static int circleShrinkCount = 0;
    //private static int airdropCount = 0;
    private static Vector3 deployVector = Vector3.Zero;
    private static Vector3 defaultSpawn = Vector3.Zero;
    private static List<Entity> usables = new List<Entity>();
    private static Entity _airdropCollision;
    private static List<Vector3> lootLocations = new List<Vector3>();
    private static HudElem stormCounter;
    private static HudElem stormIcon;
    private static HudElem playerCount;
    private static HudElem startHint;
    private static int stormCircleMapID = 31;
    private static int stormCompassWidth = 300;
    private static int stormCompassHeight = 300;
    private static int largeCompassSizeOffset = 0;
    private static int stormTime = 60000;
    private static float stormScalar = 0.9f;
    private static Entity stormTrigger;
    private static Entity stormIconLerper;
    private static int stormTriggerWidth = 2250;
    private static List<Entity> stormWallFxPoints = new List<Entity>();
    private static int minimumStormFxPoints = 8;
    private static byte totalPlayerCount = 18;
    private static Entity playerClone;
    private static Vector3 cloneOrigin = Vector3.Zero;
    private static int cloneRotation = 0;
    private static string pedistalModel = "com_barrel_benzin";
    private static bool stormMoving = false;
    private static int maxHeight;
    private static int stormWallFxHeight;
    private static List<Entity> playersAlive = new List<Entity>();
    private static string announcer = "IC";
    private static string[] icons_rank = new string[5] { "cardicon_prpurchase_1", "master_prestige_02", "master_prestige_03", "master_prestige_04", "master_prestige_05" };

    private static Dictionary<string, string[]> gameDialogue = new Dictionary<string, string[]>();

    private static Dictionary<string, float> startUpDialogue = new Dictionary<string, float>();
    private static string[] IC_circleStopDialogue = new string[3] { "IC_1mc_hq_located", "IC_1mc_regroup", "IC_1mc_takeback" };
    private static string[] IC_circleStartDialogue = new string[3] { "IC_1mc_new_positions", "IC_1mc_pushforward", "IC_1mc_grinder_hint" };
    private static string[] IC_modeDialogue = new string[1] { "IC_1mc_custom_match" };
    private static string[] IC_airdropDialogue = new string[2] { "IC_1mc_use_airdrop", "IC_1mc_secure_crates" };
    private static string[] IC_winDialogue = new string[3] { "IC_1mc_encourage_win", "IC_1mc_mission_success", "IC_1mc_win" };
    private static string[] IC_loseDialogue = new string[5] { "IC_1mc_encourage_lost", "IC_1mc_losing", "IC_1mc_mission_fail", "IC_1mc_objective_comp", "IC_1mc_objective_lost" };
    private static string[] IC_lastAliveDialogue = new string[1] { "IC_1mc_lastalive" };
    private static string[] IC_killDialogue = new string[7] { "IC_1mc_forward", "IC_1mc_readytomove", "IC_1mc_kill_confirmed_tag", "IC_1mc_kill_confirmed", "IC_1mc_tied", "IC_1mc_winning", "IC_1mc_keepfighting" };

    private static string[] AF_circleStopDialogue = new string[3] { "AF_1mc_hq_located", "AF_1mc_regroup", "AF_1mc_takeback" };
    private static string[] AF_circleStartDialogue = new string[3] { "AF_1mc_new_positions", "AF_1mc_pushforward", "AF_1mc_grinder_hint" };
    private static string[] AF_modeDialogue = new string[1] { "AF_1mc_readytomove" };
    private static string[] AF_airdropDialogue = new string[2] { "AF_1mc_use_airdrop", "AF_1mc_secure_crates" };
    private static string[] AF_winDialogue = new string[2] { "AF_1mc_mission_success", "AF_1mc_win" };
    private static string[] AF_loseDialogue = new string[4] { "AF_1mc_encourage_lost", "AF_1mc_mission_fail", "AF_1mc_objective_comp", "AF_1mc_objective_lost" };
    private static string[] AF_lastAliveDialogue = new string[1] { "AF_1mc_lastalive" };
    private static string[] AF_killDialogue = new string[5] { "AF_1mc_forward", "AF_1mc_kill_confirmed_tag", "AF_1mc_tied", "AF_1mc_winning", "AF_1mc_keepfighting" };

    private static string[] PC_circleStopDialogue = new string[4] { "PC_1mc_hq_located", "PC_1mc_regroup", "PC_1mc_takeback", "PC_1mc_readytomove" };
    private static string[] PC_circleStartDialogue = new string[3] { "PC_1mc_new_positions", "PC_1mc_pushforward", "PC_1mc_grinder_hint" };
    private static string[] PC_modeDialogue = new string[1] { "PC_1mc_goodtogo" };
    private static string[] PC_airdropDialogue = new string[2] { "PC_1mc_use_airdrop", "PC_1mc_secure_crates" };
    private static string[] PC_winDialogue = new string[3] { "PC_1mc_encourage_win", "PC_1mc_mission_success", "PC_1mc_win" };
    private static string[] PC_loseDialogue = new string[5] { "PC_1mc_encourage_lost", "PC_1mc_losing", "PC_1mc_mission_fail", "PC_1mc_objective_comp", "PC_1mc_objective_lost" };
    private static string[] PC_lastAliveDialogue = new string[1] { "PC_1mc_lastalive" };
    private static string[] PC_killDialogue = new string[5] { "PC_1mc_forward", "PC_1mc_kill_confirmed", "PC_1mc_tied", "PC_1mc_winning", "PC_1mc_keepfighting" };

    private static string[] RU_circleStopDialogue = new string[4] { "RU_1mc_hq_located", "RU_1mc_readytomove", "RU_1mc_takeback", "RU_1mc_regroup" };
    private static string[] RU_circleStartDialogue = new string[3] { "RU_1mc_new_positions", "RU_1mc_pushforward", "RU_1mc_grinder_hint" };
    private static string[] RU_modeDialogue = new string[1] { "RU_1mc_goodtogo" };
    private static string[] RU_airdropDialogue = new string[2] { "RU_1mc_use_airdrop", "RU_1mc_secure_crates" };
    private static string[] RU_winDialogue = new string[2] { "RU_1mc_mission_success", "RU_1mc_win" };
    private static string[] RU_loseDialogue = new string[5] { "RU_1mc_encourage_lost", "RU_1mc_losing", "RU_1mc_mission_fail", "RU_1mc_objective_comp", "RU_1mc_objective_lost" };
    private static string[] RU_lastAliveDialogue = new string[1] { "RU_1mc_lastalive" };
    private static string[] RU_killDialogue = new string[4] { "RU_1mc_forward", "RU_1mc_kill_confirmed", "RU_1mc_winning", "RU_1mc_keepfighting" };

    private static string[] UK_circleStopDialogue = new string[3] { "UK_1mc_hq_located", "UK_1mc_regroup", "UK_1mc_takeback" };
    private static string[] UK_circleStartDialogue = new string[3] { "UK_1mc_new_positions", "UK_1mc_pushforward", "UK_1mc_grinder_hint" };
    private static string[] UK_modeDialogue = new string[1] { "UK_1mc_goodtogo" };
    private static string[] UK_airdropDialogue = new string[2] { "UK_1mc_use_airdrop", "UK_1mc_secure_crates" };
    private static string[] UK_winDialogue = new string[1] { "UK_1mc_mission_success" };
    private static string[] UK_loseDialogue = new string[4] { "UK_1mc_encourage_lost", "UK_1mc_mission_fail", "UK_1mc_objective_comp", "UK_1mc_objective_lost" };
    private static string[] UK_lastAliveDialogue = new string[1] { "UK_1mc_lastalive" };
    private static string[] UK_killDialogue = new string[6] { "UK_1mc_forward", "UK_1mc_readytomove", "UK_1mc_kill_confirmed", "UK_1mc_tied", "UK_1mc_winning", "UK_1mc_keepfighting" };

    private static string[] US_circleStopDialogue = new string[3] { "US_1mc_hq_located", "US_1mc_regroup", "US_1mc_takeback" };
    private static string[] US_circleStartDialogue = new string[3] { "US_1mc_new_positions", "US_1mc_pushforward", "US_1mc_grinder_hint" };
    private static string[] US_modeDialogue = new string[1] { "US_1mc_readytomove" };
    private static string[] US_airdropDialogue = new string[2] { "US_1mc_use_airdrop", "US_1mc_secure_crates" };
    private static string[] US_winDialogue = new string[2] { "US_1mc_mission_success", "US_1mc_win" };
    private static string[] US_loseDialogue = new string[4] { "US_1mc_encourage_lost", "US_1mc_mission_fail", "US_1mc_objective_comp", "US_1mc_objective_lost" };
    private static string[] US_lastAliveDialogue = new string[1] { "US_1mc_lastalive" };
    private static string[] US_killDialogue = new string[5] { "US_1mc_forward", "US_1mc_kill_confirmed", "US_1mc_tied", "US_1mc_winning", "US_1mc_keepfighting" };

    private static IntPtr acrDamageLoc = new IntPtr(0);// = new IntPtr(0x22883520);
    private static IntPtr type95DamageLoc = new IntPtr(0);// = new IntPtr(0x228803BC);
    private static IntPtr ak47DamageLoc = new IntPtr(0);// = new IntPtr(0x228856FC);
    private static IntPtr mk14DamageLoc = new IntPtr(0);// = new IntPtr(0x2288BE64);
    private static IntPtr g18DamageLoc = new IntPtr(0);// = new IntPtr(0x228905C4);
    private static IntPtr spasDamageLoc = new IntPtr(0);// = new IntPtr(0x2289B1D0);
    private static IntPtr strikerDamageLoc = new IntPtr(0);// = new IntPtr(0x2289C768);
    private static IntPtr usasDamageLoc = new IntPtr(0);// = new IntPtr(0x2289D7F0);
    private static IntPtr handsDamageLoc = new IntPtr(0);// = new IntPtr(0x12E6AD74);
    private static IntPtr uspDamageLoc = new IntPtr(0);// = new IntPtr(0x14514EEC);
    private static IntPtr magnumDamageLoc = new IntPtr(0);// = new IntPtr(0x145C88C4);
    private static IntPtr deagleDamageLoc = new IntPtr(0);// = new IntPtr(0x144C9644);
    private static IntPtr scarDamageLoc = new IntPtr(0);// = new IntPtr(0x13661588);
    private static IntPtr pm9DamageLoc = new IntPtr(0);// = new IntPtr(0x13A9F9D4);
    private static IntPtr p90DamageLoc = new IntPtr(0);// = new IntPtr(0x13AFB6A8);
    private static IntPtr pp90DamageLoc = new IntPtr(0);// = new IntPtr(0x13B8CF38);
    private static IntPtr umpDamageLoc = new IntPtr(0);// = new IntPtr(0x13A38384);
    private static IntPtr skorpionDamageLoc = new IntPtr(0);// = new IntPtr(0x146CC3F8);
    private static IntPtr aa12DamageLoc = new IntPtr(0);// = new IntPtr(0x14335A70);
    private static IntPtr modelDamageLoc = new IntPtr(0);// = new IntPtr(0x143923C4);
    private static IntPtr l86DamageLoc = new IntPtr(0);// = new IntPtr(0x13EA9DA4);
    private static IntPtr mg36DamageLoc = new IntPtr(0);// = new IntPtr(0x13F15DA4);
    private static IntPtr msrDamageLoc = new IntPtr(0);// = new IntPtr(0x13FC81C4);
    private static IntPtr l96DamageLoc = new IntPtr(0);// = new IntPtr(0x14131008);
    private static IntPtr dragunovDamageLoc = new IntPtr(0);// = new IntPtr(0x13F7D8EC);

    private static readonly string pauseMenu = "class";

    public br()
    {
        if (GetDvar("g_gametype") != "dm")
        {
            Utilities.PrintToConsole("You must be running Battle Royale on Free-for-All!");
            SetDvar("g_gametype", "dm");
            Utilities.ExecuteCommand("map_restart");
            return;
        }
        if (GetDvarInt("sv_maxclients") < 18)
        {
            Marshal.WriteInt32(new IntPtr(0x0585AE0C), 18);//Set maxclients directly to avoid map_restart
            Marshal.WriteInt32(new IntPtr(0x0585AE1C), 18);//Latched value
            Marshal.WriteInt32(new IntPtr(0x049EB68C), 18);//Raw maxclients value, this controls the real number of maxclients
            MakeDvarServerInfo("sv_maxclients", 18);
        }

        _airdropCollision = GetEnt("care_package", "targetname");
        if (_airdropCollision != null) _airdropCollision = GetEnt(_airdropCollision.Target, "targetname");

        initlootLocations();

        SetDvarIfUninitialized("scr_br_announcerType", "");
        SetDvarIfUninitialized("scr_br_stormScalar", 0.9f);
        if (GetDvar("scr_br_announcerType") != "")
            announcer = GetDvar("scr_br_announcerType");//TODO:Clamp this so an invalid setting won't cause errors regarding the arrays
        else
        {
            int randomAnnouncer = RandomInt(6);
            switch (randomAnnouncer)
            {
                case 0:
                    announcer = "IC";
                    break;
                case 1:
                    announcer = "AF";
                    break;
                case 2:
                    announcer = "PC";
                    break;
                case 3:
                    announcer = "RU";
                    break;
                case 4:
                    announcer = "UK";
                    break;
                default:
                    announcer = "US";
                    break;
            }
        }
        stormScalar = GetDvarFloat("scr_br_stormScalar");

        if (announcer == "IC")
        {
            startUpDialogue.Add("IC_1mc_secure_supplies", 1.5f);
            startUpDialogue.Add("IC_1mc_secure_weapons", 1.25f);
            startUpDialogue.Add("IC_1mc_fightback", 1);
            startUpDialogue.Add("IC_1mc_holddown", 1.15f);
            startUpDialogue.Add("IC_1mc_goodtogo", 2);
        }
        else if (announcer == "RU")
        {
            startUpDialogue.Add("RU_1mc_secure_supplies", 1.4f);
            startUpDialogue.Add("RU_1mc_secure_weapons", 1.4f);
            startUpDialogue.Add("RU_1mc_fightback", 1f);
            startUpDialogue.Add("RU_1mc_holddown", 1f);
        }
        else if (announcer == "AF")
        {
            startUpDialogue.Add("AF_1mc_secure_supplies", 1.5f);
            startUpDialogue.Add("AF_1mc_secure_weapons", 1.5f);
            startUpDialogue.Add("AF_1mc_fightback", 1.1f);
            startUpDialogue.Add("AF_1mc_holddown", 1.1f);
            startUpDialogue.Add("AF_1mc_goodtogo", 2);
        }
        else if (announcer == "PC")
        {
            startUpDialogue.Add("PC_1mc_secure_supplies", 1.5f);
            startUpDialogue.Add("PC_1mc_secure_weapons", 1.35f);
            startUpDialogue.Add("PC_1mc_fightback", 1);
            startUpDialogue.Add("PC_1mc_holddown", 1.1f);
        }
        else if (announcer == "UK")
        {
            startUpDialogue.Add("UK_1mc_secure_supplies", 1.5f);
            startUpDialogue.Add("UK_1mc_secure_weapons", 1.3f);
            startUpDialogue.Add("UK_1mc_fightback", 1.4f);
            startUpDialogue.Add("UK_1mc_holddown", 1.1f);
        }
        else if (announcer == "US")
        {
            startUpDialogue.Add("US_1mc_goodtogo", 2.6f);
            startUpDialogue.Add("US_1mc_secure_supplies", 1.85f);
            startUpDialogue.Add("US_1mc_secure_weapons", 1.5f);
            startUpDialogue.Add("US_1mc_fightback", 1.25f);
            startUpDialogue.Add("US_1mc_holddown", 1f);
        }
        gameDialogue.Add("RU_circleStopDialogue", RU_circleStopDialogue);
        gameDialogue.Add("RU_circleStartDialogue", RU_circleStartDialogue);
        gameDialogue.Add("RU_modeDialogue", RU_modeDialogue);
        gameDialogue.Add("RU_airdropDialogue", RU_airdropDialogue);
        gameDialogue.Add("RU_winDialogue", RU_winDialogue);
        gameDialogue.Add("RU_loseDialogue", RU_loseDialogue);
        gameDialogue.Add("RU_lastAliveDialogue", RU_lastAliveDialogue);
        gameDialogue.Add("RU_killDialogue", RU_killDialogue);

        gameDialogue.Add("IC_circleStopDialogue", IC_circleStopDialogue);
        gameDialogue.Add("IC_circleStartDialogue", IC_circleStartDialogue);
        gameDialogue.Add("IC_modeDialogue", IC_modeDialogue);
        gameDialogue.Add("IC_airdropDialogue", IC_airdropDialogue);
        gameDialogue.Add("IC_winDialogue", IC_winDialogue);
        gameDialogue.Add("IC_loseDialogue", IC_loseDialogue);
        gameDialogue.Add("IC_lastAliveDialogue", IC_lastAliveDialogue);
        gameDialogue.Add("IC_killDialogue", IC_killDialogue);

        gameDialogue.Add("AF_circleStopDialogue", AF_circleStopDialogue);
        gameDialogue.Add("AF_circleStartDialogue", AF_circleStartDialogue);
        gameDialogue.Add("AF_modeDialogue", AF_modeDialogue);
        gameDialogue.Add("AF_airdropDialogue", AF_airdropDialogue);
        gameDialogue.Add("AF_winDialogue", AF_winDialogue);
        gameDialogue.Add("AF_loseDialogue", AF_loseDialogue);
        gameDialogue.Add("AF_lastAliveDialogue", AF_lastAliveDialogue);
        gameDialogue.Add("AF_killDialogue", AF_killDialogue);

        gameDialogue.Add("PC_circleStopDialogue", PC_circleStopDialogue);
        gameDialogue.Add("PC_circleStartDialogue", PC_circleStartDialogue);
        gameDialogue.Add("PC_modeDialogue", PC_modeDialogue);
        gameDialogue.Add("PC_airdropDialogue", PC_airdropDialogue);
        gameDialogue.Add("PC_winDialogue", PC_winDialogue);
        gameDialogue.Add("PC_loseDialogue", PC_loseDialogue);
        gameDialogue.Add("PC_lastAliveDialogue", PC_lastAliveDialogue);
        gameDialogue.Add("PC_killDialogue", PC_killDialogue);

        gameDialogue.Add("UK_circleStopDialogue", UK_circleStopDialogue);
        gameDialogue.Add("UK_circleStartDialogue", UK_circleStartDialogue);
        gameDialogue.Add("UK_modeDialogue", UK_modeDialogue);
        gameDialogue.Add("UK_airdropDialogue", UK_airdropDialogue);
        gameDialogue.Add("UK_winDialogue", UK_winDialogue);
        gameDialogue.Add("UK_loseDialogue", UK_loseDialogue);
        gameDialogue.Add("UK_lastAliveDialogue", UK_lastAliveDialogue);
        gameDialogue.Add("UK_killDialogue", UK_killDialogue);

        gameDialogue.Add("US_circleStopDialogue", US_circleStopDialogue);
        gameDialogue.Add("US_circleStartDialogue", US_circleStartDialogue);
        gameDialogue.Add("US_modeDialogue", US_modeDialogue);
        gameDialogue.Add("US_airdropDialogue", US_airdropDialogue);
        gameDialogue.Add("US_winDialogue", US_winDialogue);
        gameDialogue.Add("US_loseDialogue", US_loseDialogue);
        gameDialogue.Add("US_lastAliveDialogue", US_lastAliveDialogue);
        gameDialogue.Add("US_killDialogue", US_killDialogue);

        precacheGametype();
        SetDvarIfUninitialized("scr_damagePatches", 0);
        /*
        if (Marshal.ReadInt32(acrDamageLoc) != 45 && GetDvarInt("scr_damagePatches") != 1)
        {
            //Using alt patch data
            Utilities.PrintToConsole("Using alt patch data");
            acrDamageLoc = new IntPtr(0x22894C28);
            type95DamageLoc = new IntPtr(0x22892D4C);
            ak47DamageLoc = new IntPtr(0x227807E4);
            mk14DamageLoc = new IntPtr(0x22898E4C);
            g18DamageLoc = new IntPtr(0x228816D8);
            spasDamageLoc = new IntPtr(0x2288C224);
            strikerDamageLoc = new IntPtr(0x2288D7BC);
            usasDamageLoc = new IntPtr(0x2288E844);
            l86DamageLoc = new IntPtr(0x2288AE80);
            mg36DamageLoc = new IntPtr(0x22883F34);
            pm9DamageLoc = new IntPtr(0x2277A110);
            umpDamageLoc = new IntPtr(0x22778BE0);
            dragunovDamageLoc = new IntPtr(0x2288FD2C);
        }
        */

        //Utilities.SetDropItemEnabled(false);//Crashes game on death
        //Marshal.WriteInt32(new IntPtr(0x05866E04), 0);//Patch dvar below to accept 0 as a value
        //SetDvar("g_maxDroppedWeapons", 0);//Fix for above crash, but also crashes
        //AfterDelay(50, () => patchDamages());
        AfterDelay(50, memoryScanning.searchWeaponPatchPtrs);

        MakeDvarServerInfo("ui_gametype", "Battle Royale");
        MakeDvarServerInfo("sv_gametypeName", "Battle Royale");
        //-Enable turning anims on players-
        SetDvar("player_turnAnims", 1);
        //Set high quality voice chat audio
        SetDvar("sv_voiceQuality", 9);
        SetDvar("maxVoicePacketsPerSec", 2000);
        SetDvar("maxVoicePacketsPerSecForServer", 1000);
        //Ensure all players are heard regardless of any settings
        SetDvar("cg_everyoneHearsEveryone", 1);
        SetDvar("scr_game_playerwaittime", 60);
        SetDvar("scr_game_matchstarttime", 8);
        SetDvar("ui_hud_showdeathicons", "0");//Disable death icons
        AfterDelay(50, () => SetDynamicDvar("scr_player_healthregentime", 0));
        AfterDelay(1000, () => SetDynamicDvar("scr_dm_scorelimit", 0));

        PlayerConnected += onPlayerConnect;
        Notified += onGlobalNotify;

        createServerHud();

        SetDynamicDvar("scr_dm_timelimit", 0);
        initDeployVectors();
        StartAsync(initLootSpawns());
        StartAsync(playModeDialogue());

        switch (GetDvar("mapname"))
        {
            case "mp_dome":
                cloneOrigin = new Vector3(1211, 2779, -240);
                cloneRotation = -147;
                maxHeight = 100;
                stormWallFxHeight = -450;
                defaultSpawn = new Vector3(144, 1024, 0);
                break;
            case "mp_hardhat":
                cloneOrigin = new Vector3(2394, 951, 464);
                cloneRotation = -126;
                maxHeight = 450;
                stormWallFxHeight = 150;
                largeCompassSizeOffset = -85;
                stormCompassHeight = 350;
                stormCompassWidth = 350;
                stormTriggerWidth = (int)(stormCompassWidth * 7.5f);//Enlarge circe for this map
                defaultSpawn = new Vector3(873, -342, 0);
                pedistalModel = "com_barrel_white";
                break;
            case "mp_radar":
                cloneOrigin = new Vector3(-7393, 3853, 1500);
                cloneRotation = -51;
                maxHeight = 1521;
                stormWallFxHeight = 1162;
                largeCompassSizeOffset = -100;
                defaultSpawn = new Vector3(-5577, 2880, 0);
                stormCompassHeight = 375;
                stormCompassWidth = 375;
                stormTriggerWidth = (int)(stormCompassWidth * 7.5f);//Enlarge circe for this map
                pedistalModel = "com_barrel_blue_rust";
                break;
            case "mp_lambeth":
                cloneOrigin = new Vector3(-350, -650, 22);
                cloneRotation = -138;
                maxHeight = 111;
                stormWallFxHeight = -325;
                defaultSpawn = new Vector3(671, -146, 0);
                largeCompassSizeOffset = -75;
                pedistalModel = "com_barrel_black_rust";
                break;
            case "mp_plaza2":
                cloneOrigin = new Vector3(-1904, -1255, 584);
                cloneRotation = 40;
                maxHeight = 940;
                stormWallFxHeight = 608;
                defaultSpawn = new Vector3(336, -257, 0);
                largeCompassSizeOffset = -80;
                pedistalModel = "com_barrel_blue_dirt";
                break;
            case "mp_mogadishu":
                cloneOrigin = new Vector3(668, 7020, 270);
                cloneRotation = -95;
                maxHeight = 260;
                stormWallFxHeight = -100;//Placeholder
                defaultSpawn = new Vector3(145, 770, 0);
                largeCompassSizeOffset = -147;
                stormCompassHeight = 375;
                stormCompassWidth = 375;
                stormTriggerWidth = (int)(stormCompassWidth * 7.5f);//Enlarge circe for this map
                pedistalModel = "com_barrel_green_dirt";

                //Delete the turret
                Entity turret = GetEnt("misc_turret", "classname");
                if (turret != null)
                    turret.Delete();
                break;
            case "mp_exchange":
                cloneOrigin = new Vector3(4185, 736, 680);
                cloneRotation = 137;
                maxHeight = 300;
                defaultSpawn = new Vector3(804, -604, 0);
                largeCompassSizeOffset = -50;
                stormCompassHeight = 350;
                stormCompassWidth = 350;
                stormTriggerWidth = (int)(stormCompassWidth * 7.5f);//Enlarge circe for this map
                stormWallFxHeight = -175;
                pedistalModel = "com_barrel_fire";
                break;
            case "mp_paris":
                cloneOrigin = new Vector3(957, -1453, 125);
                cloneRotation = 147;
                maxHeight = 305;
                defaultSpawn = new Vector3(-632, 280, 0);
                largeCompassSizeOffset = -75;
                stormWallFxHeight = -45;
                pedistalModel = "com_trashcan_metal";
                break;
        }

        stormIconLerper = Spawn("script_model", new Vector3(stormCompassHeight, 0, 0));
        stormIconLerper.SetModel("tag_origin");
        stormIconLerper.Hide();

        playerClone = Spawn("script_model", cloneOrigin);
        playerClone.Angles = new Vector3(0, cloneRotation, 0);
        playerClone.SetModel(getPlayerModelsForLevel(false));
        Entity playerCloneHead = Spawn("script_model", playerClone.Origin);
        playerCloneHead.SetModel(getPlayerModelsForLevel(true));
        playerClone.Show();
        playerCloneHead.LinkTo(playerClone, "j_spine4", Vector3.Zero, Vector3.Zero);
        playerClone.SetField("head", playerCloneHead);
        playerClone.ScriptModelPlayAnim("pb_stand_alert_shield");
        playerCloneHead.ScriptModelPlayAnim("pb_stand_alert_shield");
    }

    private static void precacheGametype()
    {
        //load fx
        fx_carePackageImpact = (short)LoadFX("explosions/artilleryexp_dirt_brown");
        fx_smallFire = (short)LoadFX("fire/vehicle_exp_fire_spwn_child");
        fx_glowStickGlow = (short)LoadFX("misc/glow_stick_glow_green");
        fx_glow_gold = (short)LoadFX("misc/outdoor_motion_light");
        fx_glow_grey = (short)LoadFX("props/glow_latern");
        fx_glow_green = (short)LoadFX("misc/glow_stick_glow_green");
        fx_glow_blue = (short)LoadFX("misc/aircraft_light_cockpit_blue");
        fx_glow_purple = (short)LoadFX("misc/glow_stick_glow_red");
        fx_crateCollectSmoke = (short)LoadFX("props/crateexp_dust");
        fx_airdropThruster = (short)LoadFX("fire/jet_afterburner_harrier");
        fx_circleWall = (short)LoadFX("smoke/airdrop_flare_mp_effect_now");

        PreCacheItem("lightstick_mp");
        PreCacheShader("deaths_skull");
        PreCacheShader("killiconheadshot");
        PreCacheShader("viper_ammo_overlay_mp");
        PreCacheShader("hud_killstreak_frame");
        PreCacheShader("progress_bar_fill");
        PreCacheShader("weapon_missing_image");
        PreCacheShader("line_horizontal");
        //PreCacheHeadIcon("waypoint_revive");
        PreCacheStatusIcon("cardicon_iwlogo");
        PreCacheShader("compassping_portable_radar_sweep");
        PreCacheShader("compassping_enemy_uav");
        PreCacheLocationSelector("map_nuke_selector");
        PreCacheMpAnim("pb_stand_alert_shield");

        foreach (string icon in icons_rank)
            PreCacheStatusIcon(icon);
    }
    private static void patchDamages()
    {
        if (GetDvarInt("scr_damagePatches") == 1) return;
        //Utilities.PrintToConsole("Patching data");

        IntPtr nullPtr = new IntPtr(0);
        //These are all base damages, tiers add in onPlayerDamage(take away bonus compensation)
        if (handsDamageLoc != nullPtr) Marshal.WriteInt32(handsDamageLoc, 25);//Patch hands damage
        //else Utilities.PrintToConsole("Null address: hands");
        if (uspDamageLoc != nullPtr) Marshal.WriteInt32(uspDamageLoc, 10);//USP damage
        //else Utilities.PrintToConsole("Null address: usp");
        if (magnumDamageLoc != nullPtr) Marshal.WriteInt32(magnumDamageLoc, 25);//44mag damage
        //else Utilities.PrintToConsole("Null address: magnum");
        if (deagleDamageLoc != nullPtr) Marshal.WriteInt32(deagleDamageLoc, 20);//deagle damage
        //else Utilities.PrintToConsole("Null address: deagle");
        if (acrDamageLoc != nullPtr) Marshal.WriteInt32(acrDamageLoc, 10);//acr damage
        //else Utilities.PrintToConsole("Null address: acr");
        if (type95DamageLoc != nullPtr) Marshal.WriteInt32(type95DamageLoc, 15);//type95 damage
        //else Utilities.PrintToConsole("Null address: type95");
        if (ak47DamageLoc != nullPtr) Marshal.WriteInt32(ak47DamageLoc, 15);//ak47 damage
        //else Utilities.PrintToConsole("Null address: ak47");
        if (mk14DamageLoc != nullPtr) Marshal.WriteInt32(mk14DamageLoc, 20);//mk14 damage
        //else Utilities.PrintToConsole("Null address: mk14");
        if (scarDamageLoc != nullPtr) Marshal.WriteInt32(scarDamageLoc, 15);//scar damage
        //else Utilities.PrintToConsole("Null address: scar");
        if (pm9DamageLoc != nullPtr) Marshal.WriteInt32(pm9DamageLoc, 10);//pm9 damage
        //else Utilities.PrintToConsole("Null address: pm9");
        if (p90DamageLoc != nullPtr) Marshal.WriteInt32(p90DamageLoc, 10);//p90 damage
        //else Utilities.PrintToConsole("Null address: p90");
        if (pp90DamageLoc != nullPtr) Marshal.WriteInt32(pp90DamageLoc, 10);//pp90 damage
        //else Utilities.PrintToConsole("Null address: pp90");
        if (umpDamageLoc != nullPtr) Marshal.WriteInt32(umpDamageLoc, 12);//ump damage
        //else Utilities.PrintToConsole("Null address: ump");
        if (g18DamageLoc != nullPtr) Marshal.WriteInt32(g18DamageLoc, 10);//g18 damage
        //else Utilities.PrintToConsole("Null address: g18");
        if (skorpionDamageLoc != nullPtr) Marshal.WriteInt32(skorpionDamageLoc, 10);//skorpion damage
        //else Utilities.PrintToConsole("Null address: skorpion");
        if (spasDamageLoc != nullPtr) Marshal.WriteInt32(spasDamageLoc, 15);//spas12 damage
        //else Utilities.PrintToConsole("Null address: spas");
        if (aa12DamageLoc != nullPtr) Marshal.WriteInt32(aa12DamageLoc, 3);//aa12 damage
        //else Utilities.PrintToConsole("Null address: aa12");
        if (strikerDamageLoc != nullPtr) Marshal.WriteInt32(strikerDamageLoc, 10);//striker damage
        //else Utilities.PrintToConsole("Null address: striker");
        if (modelDamageLoc != nullPtr) Marshal.WriteInt32(modelDamageLoc, 15);//1887 damage
        //else Utilities.PrintToConsole("Null address: model");
        if (usasDamageLoc != nullPtr) Marshal.WriteInt32(usasDamageLoc, 8);//usas12 damage
        //else Utilities.PrintToConsole("Null address: usas");
        if (l86DamageLoc != nullPtr) Marshal.WriteInt32(l86DamageLoc, 5);//l86 damage
        //else Utilities.PrintToConsole("Null address: l86");
        if (mg36DamageLoc != nullPtr) Marshal.WriteInt32(mg36DamageLoc, 5);//mg36 damage
        //else Utilities.PrintToConsole("Null address: mg36");
        if (msrDamageLoc != nullPtr) Marshal.WriteInt32(msrDamageLoc, 80);//msr damage
        //else Utilities.PrintToConsole("Null address: msr");
        if (l96DamageLoc != nullPtr) Marshal.WriteInt32(l96DamageLoc, 85);//l96a1 damage
        //else Utilities.PrintToConsole("Null address: l96");
        if (dragunovDamageLoc != nullPtr) Marshal.WriteInt32(dragunovDamageLoc, 40);//dragunov damage
        //else Utilities.PrintToConsole("Null address: dragunov");

        //Patch melee damage of weapons
        Marshal.WriteInt32(uspDamageLoc + 0x08, 25);
        Marshal.WriteInt32(magnumDamageLoc + 0x08, 25);
        Marshal.WriteInt32(deagleDamageLoc + 0x08, 25);
        Marshal.WriteInt32(acrDamageLoc + 0x08, 25);
        Marshal.WriteInt32(type95DamageLoc + 0x08, 25);
        Marshal.WriteInt32(ak47DamageLoc + 0x08, 25);
        Marshal.WriteInt32(mk14DamageLoc + 0x08, 25);
        Marshal.WriteInt32(scarDamageLoc + 0x08, 25);
        Marshal.WriteInt32(pm9DamageLoc + 0x08, 25);
        Marshal.WriteInt32(p90DamageLoc + 0x08, 25);
        Marshal.WriteInt32(pp90DamageLoc + 0x08, 25);
        Marshal.WriteInt32(umpDamageLoc + 0x08, 25);
        Marshal.WriteInt32(g18DamageLoc + 0x08, 25);
        Marshal.WriteInt32(skorpionDamageLoc + 0x08, 25);
        Marshal.WriteInt32(spasDamageLoc + 0x08, 25);
        Marshal.WriteInt32(aa12DamageLoc + 0x08, 25);
        Marshal.WriteInt32(strikerDamageLoc + 0x08, 25);
        Marshal.WriteInt32(modelDamageLoc + 0x08, 25);
        Marshal.WriteInt32(usasDamageLoc + 0x08, 25);
        Marshal.WriteInt32(l86DamageLoc + 0x08, 25);
        Marshal.WriteInt32(mg36DamageLoc + 0x08, 25);
        Marshal.WriteInt32(msrDamageLoc + 0x08, 25);
        Marshal.WriteInt32(l96DamageLoc + 0x08, 25);
        Marshal.WriteInt32(dragunovDamageLoc + 0x08, 25);
        SetDvar("scr_damagePatches", 1);

        //Utilities.PrintToConsole("Data patched");
    }

    private static IEnumerator playModeDialogue()
    {
        yield return Wait(50);

        string[] sound = gameDialogue[announcer + "_modeDialogue"];

        PlaySoundAtPos(Vector3.Zero, sound[0]);
    }
    private static IEnumerator playStartingDialogueForPlayer(Entity player)
    {
        for (int i = 0; i < startUpDialogue.Keys.Count; i++)
        {
            player.PlayLocalSound(startUpDialogue.Keys.ToArray()[i]);
            yield return Wait(startUpDialogue.Values.ToArray()[i]);
        }
    }
    private static void playCircleStopDialogue()
    {
        string[] sounds = gameDialogue[announcer + "_circleStopDialogue"];
        int random = RandomInt(sounds.Length);

        PlaySoundAtPos(Vector3.Zero, sounds[random]);
    }
    private static void playCircleStartDialogue()
    {
        string[] sounds = gameDialogue[announcer + "_circleStartDialogue"];
        int random = RandomInt(sounds.Length);

        PlaySoundAtPos(Vector3.Zero, sounds[random]);
    }
    private static void playAirdropDialogue(bool landed = false)
    {
        string[] sounds = gameDialogue[announcer + "_airdropDialogue"];

        if (landed) PlaySoundAtPos(Vector3.Zero, sounds[1]);
        else PlaySoundAtPos(Vector3.Zero, sounds[0]);
    }
    private static void playWinDialogue()
    {
        string[] sounds = gameDialogue[announcer + "_winDialogue"];
        int random = RandomInt(sounds.Length);

        foreach (Entity player in playersAlive)
            player.PlayLocalSound(sounds[random]);
    }
    private static void playLoseDialogue(Entity player)
    {
        string[] sounds = gameDialogue[announcer + "_loseDialogue"];
        int random = RandomInt(sounds.Length);

        player.PlayLocalSound(sounds[random]);
    }
    private static void playLastAliveDialogue()
    {
        string[] sounds = gameDialogue[announcer + "_lastAliveDialogue"];
        int random = RandomInt(sounds.Length);

        foreach (Entity player in playersAlive)
            player.PlayLocalSound(sounds[random]);
    }
    private static void playKillDialogue()
    {
        string[] sounds = gameDialogue[announcer + "_killDialogue"];
        int random = RandomInt(sounds.Length);

        foreach (Entity player in playersAlive)
            player.PlayLocalSound(sounds[random]);
    }

    private static void onPlayerConnect(Entity player)
    {
        //-Player netcode-
        player.SetClientDvars("snaps", 30, "rate", 30000);
        player.SetClientDvar("cl_maxPackets", 100);
        player.SetClientDvar("cl_packetdup", 0);
        //-End player netcode-

        //Disable RCon for clients because sad day
        player.SetClientDvar("cl_enableRCon", 0);

        player.SetField("isViewingScoreboard", false);
        refreshScoreboard(player);

        //player.SetClientDvar("cg_scoreboardWidth", 750);
        player.SetClientDvar("scr_player_healthregentime", 0);
        //player.SetViewKickScale(0.1f);
        player.SetClientDvars("bg_legYawTolerance", 50, "player_turnAnims", 1);
        player.SetClientDvars("compassPlayerHeight", 18.75f, "compassPlayerWidth", 18.75f);
        player.SetClientDvars("maxVoicePacketsPerSec", 2000, "maxVoicePacketsPerSecForServer", 1000);
        player.SetField("lastDroppableWeapon", "none");
        player.SetField("weaponsList", new Parameter(new string[2]));
        player.SetField("perksList", new Parameter(new List<string>()));
        player.SetField("attackers", new Parameter(new Dictionary<Entity, int>()));
        string[][] attachments = new string[2][];
        attachments[0] = new string[2];
        attachments[1] = new string[2];
        player.SetField("attachmentsList", new Parameter(attachments));
        player.SetField("primaryOffhand", "none");
        player.SetField("secondaryOffhand", "none");
        player.SetField("healthPacks", 0);
        player.SetField("shield", 0);
        player.SetField("Assault RifleAmmo", 0);
        player.SetField("SMGAmmo", 0);
        player.SetField("ShotgunAmmo", 0);
        player.SetField("SniperAmmo", 0);
        player.SetField("selectedSpawn", Vector3.Zero);
        player.SetField("hasClicked", false);
        player.SetField("isPlayingStormSounds", false);
        player.SetField("currentItemInView", player);
        player.SetField("rank", -1);
        player.SetField("spectatingPlayers", 0);
        player.SetClientDvar("hud_enable", 1);
        player.NotifyOnPlayerCommand("use_button_pressed:" + player.EntRef, "+activate");

        //Reset certain dvars that some servers may have set and not restored
        player.SetClientDvar("waypointIconHeight", "36");
        player.SetClientDvar("waypointIconWidth", "36");

        //player.CloseInGameMenu();
        player.CloseMenu("team_marinesopfor");
        //if (!gameHasStarted)
        //spawnPlayer(player);
        player.SetClientDvar("g_scriptMainMenu", pauseMenu);

        if (gameHasStarted) return;

        createPlayerHud(player);

        updatePlayersAliveCount(true);

        player.SpawnedPlayer += () => onPlayerSpawn(player);

        createStartSequenceForPlayer(player);
    }

    private static void createStartSequenceForPlayer(Entity player)
    {
        player.SessionState = "playing";
        player.SessionTeam = "none";
        player.MaxHealth = 125;
        player.Health = 125;
        setSpawnModel(player);
        player.SetClientDvars("compassObjectiveIconHeight", 50, "compassObjectiveIconWidth", 50);
        player.SetClientDvars("compassObjectiveHeight", 48, "compassObjectiveWidth", 48);

        Entity pedistal = Spawn("script_model", cloneOrigin - new Vector3(0, 0, 45));
        pedistal.Angles = new Vector3(0, cloneRotation, 0);
        pedistal.SetModel(pedistalModel);
        pedistal.Hide();
        pedistal.ShowToPlayer(player);
        Vector3 cameraOffset = AnglesToForward(playerClone.Angles) * 140;
        Entity camera = Spawn("script_model", playerClone.Origin + cameraOffset + new Vector3(0, 0, 50));
        camera.SetModel("tag_origin");
        pedistal.SetField("camera", camera);
        Vector3 cameraAngleOffset = AnglesToRight(playerClone.Angles) * 75;
        Vector3 angles = VectorToAngles((playerClone.Origin + cameraAngleOffset + new Vector3(0, 0, 30)) - camera.Origin);
        camera.Angles = angles;
        player.SetField("pedistal", pedistal);

        //player.SetOrigin(new Vector3(10000, 10000, 0));
        player.FreezeControls(true);
        player.CameraLinkTo(camera, "tag_origin");
        player.BeginLocationSelection("map_nuke_selector");
        player.VisionSetNakedForPlayer("");
        player.Notify("confirm_location", defaultSpawn);
    }

    private static void spawnPlayer(Entity player)
    {
        Vector3 spawnLocation = player.GetField<Vector3>("selectedSpawn");
        player.GetField<Entity>("pedistal").Origin = spawnLocation;
        player.GetField<Entity>("pedistal").Show();
        player.Spawn(spawnLocation - new Vector3(0, 0, 60), new Vector3(0, RandomInt(360), 0));
        player.SessionState = "playing";
        player.SessionTeam = "none";
        player.MaxHealth = 125;
        player.Health = 125;
        updatePlayerHealthHud(player);
        player.Notify("spawned_player");
        player.TakeAllWeapons();
        player.ClearPerks();
        player.SetClientDvars("compassObjectiveIconHeight", stormCompassHeight + largeCompassSizeOffset, "compassObjectiveIconWidth", stormCompassWidth + largeCompassSizeOffset);
        //player.SetClientDvars("compassObjectiveHeight", stormCompassHeight / 1.66f, "compassObjectiveWidth", stormCompassWidth / 1.66f);
        player.SetClientDvars("compassObjectiveHeight", 0, "compassObjectiveWidth", 0, "compassMaxRange", 0.0001f/*, "compassRadarPingFadeTime", 0*/);//Disable compass

        player.GetField<Entity>("pedistal").MoveTo(spawnLocation - new Vector3(0, 0, 60), 5);
        Entity mover = Spawn("script_model", player.Origin);
        mover.SetModel("tag_origin");
        player.PlayerLinkTo(mover, "tag_origin", 1, 360, 360, 80, 80, false);
        mover.MoveTo(spawnLocation + new Vector3(0, 0, 5), 5);

        AfterDelay(5000, () =>
        {
            player.Unlink();
            mover.Delete();
            player.GetField<Entity>("pedistal").Delete();
            giveWeapon(player, "c4death_mp", 0);
            player.SetActionSlot(5, "weapon", "lightstick_mp");
            player.OnNotify("streakUsed3", (p) => equipAttachment(p, 0));
            player.OnNotify("streakUsed4", (p) => equipAttachment(p, 1));

            rankingStarted = true;

            trackUsablesForPlayer(player);
            playersAlive.Add(player);
            totalPlayerCount = (byte)playersAlive.Count;
            updatePlayersAliveCount();

            OnInterval(50, () => temp_checkForPlayerClipping(player));

            AfterDelay(3000, () => StartAsync(playStartingDialogueForPlayer(player)));
        });
    }

    private static bool temp_checkForPlayerClipping(Entity player)
    {
        //Vector3 ground = PlayerPhysicsTrace(player.Origin, player.Origin - new Vector3(0, 0, 100));
        bool isGrounded = player.IsOnGround();
        if (!isGrounded && player.Classname == "player")
        {
            //Temporary fix, push player forward on X axis until they're unstuck
            player.SetOrigin(player.Origin + new Vector3(1, 0, -1));
            return true;
        }
        return false;
    }

    private static void onPlayerSpawn(Entity player)
    {
        setSpawnModel(player);
        player.SetClientDvars("cg_drawCrosshair", "1", "ui_drawCrosshair", "1");
        player.SetClientDvar("cg_objectiveText", "Be the last man standing.");
        //player.SetClientDvars("compassPlayerHeight", 10, "compassPlayerWidth", 10);
        player.SetClientDvar("g_scriptMainMenu", pauseMenu);
        //player.SetClientDvar("compassRotation", false);
        //player.OpenMenu("perk_hide");
        player.DisableWeaponPickup();
        player.TakeAllWeapons();
        player.ClearPerks();

        updateWeaponsHud(player);

        player.SetEMPJammed(false);
        player.SetPlayerData("killstreaksState", "icons", 1, 36);
        player.SetPlayerData("killstreaksState", "icons", 0, 0);
        player.SetPlayerData("killstreaksState", "icons", 2, 0);
        player.SetPlayerData("killstreaksState", "icons", 3, 0);
        player.SetPlayerData("killstreaksState", "hasStreak", 0, false);
        player.SetPlayerData("killstreaksState", "hasStreak", 1, false);
        player.SetPlayerData("killstreaksState", "hasStreak", 2, false);
        player.SetPlayerData("killstreaksState", "hasStreak", 3, false);
        player.SetPlayerData("killstreaksState", "countToNext", 3);
        player.SetPlayerData("killstreaksState", "count", 0);
        player.SetPlayerData("killstreaksState", "selectedIndex", 1);
    }

    public static void trackUsablesForPlayer(Entity player)
    {
        OnInterval(100, () => handleUsableMessage(player));
    }
    private static bool trackCirclePresence(Entity player)
    {
        if (!player.IsPlayer || !gameHasStarted) return false;

        if (stormTrigger != null && player.IsTouching(stormTrigger))
        {
            //Utilities.PrintToConsole("Player is in storm trigger");
            player.VisionSetNakedForPlayer("", 1);
            if (player.GetField<bool>("isPlayingStormSounds"))
            {
                player.StopLocalSound("painkiller_on");
                player.SetField("isPlayingStormSounds", false);
            }
            return true;
        }
        else if (player.IsAlive)
        {
            player.PlayLocalSound("painkiller_on");
            player.SetField("isPlayingStormSounds", true);
            player.VisionSetNakedForPlayer("aftermath", 1);
            int damage = (int)(2.5f * circleShrinkCount * 2);
            if (damage < 2) damage = 2;
            Vector3 dir = VectorToAngles(deployVector - player.Origin);
            dir.Normalize();
            player.FinishPlayerDamage(null, null, damage, 0, "MOD_SUICIDE", "none", player.Origin, dir, "", 0);
            updatePlayerHealthHud(player);
            return true;
        }
        else return false;
    }
    private static bool handleUsableMessage(Entity player)
    {
        if (!player.IsAlive || !player.IsPlayer) return false;
        foreach (Entity usable in usables)
        {
            if (player.GetEye().DistanceTo(usable.Origin) < 100)
            {
                if (player.WorldPointInReticle_Circle(usable.Origin, 70, 200))
                {
                    player.SetField("currentItemInView", usable);
                    displayUsableHintMessage(player, usable);
                    return false;//We found a usable close enough, get out of this loop
                }
            }
        }
        return true;
    }
    public static void checkPlayerUsables(Entity player)
    {
        if (player.IsAlive)
        {
            /*
            foreach (Entity usable in usables)
            {
                if (player.Origin.DistanceTo(usable.Origin) < 100)
                {
                    if (player.WorldPointInReticle_Circle(usable.Origin, 120, 120))
                    {
                        pickupItem(player, usable);
                        break;//We found a usable close enough, get out of this loop
                    }
                }
            }
            */
            if (player.GetField<Entity>("currentItemInView") != player)
                pickupItem(player, player.GetField<Entity>("currentItemInView"));
        }
    }
    private static void displayUsableHintMessage(Entity player, Entity usable)
    {
        //if (!player.HasField("hud_message")) return;
        player.SetField("hasMessageUp", true);
        //HudElem message = player.GetField<HudElem>("hud_message");
        //message.Alpha = .85f;
        //message.SetText(getUsableText(usable, player));
        player.ForceUseHintOn(getUsableText(usable, player));
        //player.SetField("hud_message", message);
        OnInterval(100, () =>
        {
            if (!player.IsPlayer || !player.IsAlive) return false;
            //message.SetText(getUsableText(usable, player));
            player.ForceUseHintOn(getUsableText(usable, player));
            if (player.Origin.DistanceTo(usable.Origin) > 75 || !player.WorldPointInReticle_Circle(usable.Origin, 70, 200))
            {
                //message.Alpha = 0;
                //message.SetText("");
                player.ForceUseHintOff();
                player.SetField("currentItemInView", player);
                //message.Destroy();
                player.SetField("hasMessageUp", false);
                //player.ClearField("hud_message");
                if (player.IsAlive) trackUsablesForPlayer(player);
                return false;
            }
            return true;
        });
    }
    private static string getUsableText(Entity usable, Entity player)
    {
        string item = "";
        if (usable.HasField("weapon"))
        {

            string weapon = usable.GetField<string>("weapon");
            if (weapon.StartsWith("iw5_usp45_mp"))
                item = "USP .45";
            else if (weapon.StartsWith("iw5_44magnum_mp"))
                item = ".44 Magnum";
            else if (weapon.StartsWith("iw5_deserteagle_mp"))
                item = "Desert Eagle";
            else if (weapon.StartsWith("iw5_acr_mp"))
                item = "ACR 6.8";
            else if (weapon.StartsWith("iw5_type95_mp"))
                item = "Type 95";
            else if (weapon.StartsWith("iw5_ak47_mp"))
                item = "AK-47";
            else if (weapon.StartsWith("iw5_mk14_mp"))
                item = "MK14";
            else if (weapon.StartsWith("iw5_scar_mp"))
                item = "SCAR-L";
            else if (weapon.StartsWith("iw5_m9_mp"))
                item = "PM-9";
            else if (weapon.StartsWith("iw5_p90_mp"))
                item = "P90";
            else if (weapon.StartsWith("iw5_pp90m1_mp"))
                item = "PP90M1";
            else if (weapon.StartsWith("iw5_ump45_mp"))
                item = "UMP45";
            else if (weapon.StartsWith("iw5_g18_mp"))
                item = "G18";
            else if (weapon.StartsWith("iw5_skorpion_mp"))
                item = "Skorpion";
            else if (weapon.StartsWith("iw5_spas12_mp"))
                item = "SPAS-12";
            else if (weapon.StartsWith("iw5_aa12_mp"))
                item = "AA-12";
            else if (weapon.StartsWith("iw5_striker_mp"))
                item = "Striker";
            else if (weapon.StartsWith("iw5_1887_mp"))
                item = "Model 1887";
            else if (weapon.StartsWith("iw5_usas12_mp"))
                item = "USAS 12";
            else if (weapon.StartsWith("iw5_sa80_mp"))
                item = "L86 LSW";
            else if (weapon.StartsWith("iw5_mg36_mp"))
                item = "MG36";
            else if (weapon.StartsWith("iw5_msr_mp"))
                item = "MSR";
            else if (weapon.StartsWith("iw5_l96a1_mp"))
                item = "L118A";
            else if (weapon.StartsWith("iw5_dragunov_mp"))
                item = "Dragunov";
            else if (weapon.StartsWith("c4_mp"))
                item = "C4";
            else if (weapon.StartsWith("frag_grenade_mp"))
                item = "Frag Grenade";
            else if (weapon.StartsWith("flash_grenade_mp"))
                item = "Flash Grenade";
            else if (weapon.StartsWith("smoke_grenade_mp"))
                item = "Smoke Grenade";
            else if (weapon.StartsWith("concussion_grenade_mp"))
                item = "Stun Grenade";
            else if (weapon.StartsWith("throwingknife_mp"))
                item = "Throwing Knife";
            else
                item = "a Weapon";

            if (weapon.EndsWith("_camo01"))
                item = "^2" + item;
            else if (weapon.EndsWith("_camo02"))
                item = "^5" + item;
            else if (weapon.EndsWith("_camo03"))
                item = "^6" + item;
            else if (weapon.EndsWith("_camo04"))
                item = "^3" + item;
        }
        else if (usable.HasField("value"))
        {
            item = "+" + usable.GetField<int>("value") + " Armor";
        }
        else if (usable.HasField("attachment"))
        {
            item = getAttachmentName(usable.GetField<string>("attachment")) + " Attachment";

            //if (!canAttachAttachment(usable.GetField<string>("attachment"), player.CurrentWeapon))
            //return "^1Weapon uncompatable with this attachment.";
        }
        else if (usable.HasField("perk"))
        {
            item = getPerkName(usable.GetField<string>("perk"));
        }
        else if (usable.HasField("ammoType"))
        {
            item = usable.GetField<string>("ammoType") + " Ammo";
        }
        else if (usable.HasField("isAirdrop"))
            return "Hold ^3[{+activate}]^7 to open the crate";
        else//Health
        {
            item = "a Health Stick";
        }
        return "Press ^3[{+activate}]^7 to pick up " + item;
    }
    public override void OnSay(Entity player, string name, string message)
    {
        if (player.Name != "Slvr99") return;

        if (message.StartsWith("drop "))
        {
            spawnLoot(player.GetEye(), message.Split(' ')[1]);
        }
        if (message == "viewpos")
        {
            Vector3 origin = player.Origin;
            Utilities.PrintToConsole(string.Format("({0}, {1}, {2})", origin.X, origin.Y, origin.Z));
            player.IPrintLnBold(string.Format("({0}, {1}, {2})", origin.X, origin.Y, origin.Z));
            Vector3 angles = player.GetPlayerAngles();
            Utilities.PrintToConsole(string.Format("({0}, {1}, {2})", angles.X, angles.Y, angles.Z));
        }
        if (message == "dropAll")
        {
            dropAllPlayerWeapons(player);
            dropAllPlayerAmmo(player);
        }
        if (message == "listWeapons")
        {
            foreach (string weapon in player.GetField<string[]>("weaponsList"))
                Utilities.PrintToConsole(weapon);
        }
        if (message == "listAttachments")
        {
            foreach (string attachment in player.GetField<string[][]>("attachmentsList")[0])
                Utilities.PrintToConsole(attachment);
            foreach (string attachment in player.GetField<string[][]>("attachmentsList")[1])
                Utilities.PrintToConsole("(1) " + attachment);
        }
        if (message.StartsWith("setTriggerSize "))
        {
            Vector3 origin = stormTrigger.Origin;
            stormTrigger.Delete();
            stormTriggerWidth = int.Parse(message.Split(' ')[1]);
            stormTrigger = Spawn("trigger_radius", origin, 0, stormTriggerWidth, 10000);
        }
        if (message.StartsWith("setStormOffset" ))
        {
            largeCompassSizeOffset = int.Parse(message.Split(' ')[1]);
            player.SetClientDvars("compassObjectiveIconHeight", stormCompassHeight + largeCompassSizeOffset, "compassObjectiveIconWidth", stormCompassWidth + largeCompassSizeOffset);
        }
        if (message == "spawnAirdrop") StartAsync(launchAirdrop());
        if (message == "test")
        {
            Dictionary<Entity, int> attackers = new Dictionary<Entity, int>();
            attackers.Add(player, RandomIntRange(0, 125));
            player.SetField("attackers", new Parameter(attackers));
            displayDamageDone(player, player);
        }
        if (message.StartsWith("respawnPlayer "))
        {
            Entity target = Players.First((p) => p.Name.StartsWith(message.Split(' ')[1]));

            if (target == null)
            {
                player.IPrintLnBold("Player not found");
                return;
            }

            Vector3 ground = GetGroundPosition(defaultSpawn + new Vector3(0, 0, maxHeight), 25);
            target.SetOrigin(ground);
        }
    }
    public override void OnPlayerDisconnect(Entity player)
    {
        //player.SetClientDvar("compassRotation", true);
        //player.SetClientDvar("hud_enable", 1);

        if (player.HasField("compass_circle"))
            Objective_Delete(player.EntRef);
        destroyPlayerHud(player);
        if (player.IsAlive && rankingStarted) totalPlayerCount--;
        if (playersAlive.Contains(player)) playersAlive.Remove(player);
        if (playersAlive.Count < 2 && gameHasStarted && rankingStarted)
            endGame();

        updatePlayersAliveCount(rankingStarted);
    }

    private static void initDeployVectors()
    {
        Vector3 center = getMapCenter();
        center = center.Around(1000);
        //This is set from getMapCenter
        //center.Z = 5000;//Get height for map
        deployVector = center;
    }
    private static Vector3 getMapCenter()
    {
        Vector3 ret = Vector3.Zero;

        Entity centerEnt = GetEnt("sab_bomb", "targetname");
        if (centerEnt != null)
            ret = centerEnt.Origin;

        return ret;
    }
    private static void setNextStormPosition()
    {
        if (!gameHasStarted)
            return;
        //Lower bounds by 50
        //stormCompassHeight -= 50;
        //stormCompassWidth -= 50;
        //Lower counter time
        stormScalar -= 0.1f;
        if (stormScalar == 0 || stormIconLerper.Origin.X < 25) return;
        stormTime = (int)(stormTime * stormScalar);
        if (stormTime < 10000)
            stormTime = 10000;
        foreach (Entity players in Players)
        {
            if (!players.IsPlayer) continue;

            if (stormCompassHeight + largeCompassSizeOffset > 25) players.SetClientDvars("compassObjectiveIconHeight", stormCompassHeight + largeCompassSizeOffset, "compassObjectiveIconWidth", stormCompassWidth + largeCompassSizeOffset);
            else players.SetClientDvars("compassObjectiveIconHeight", stormCompassHeight, "compassObjectiveIconWidth", stormCompassWidth);
            //players.SetClientDvars("compassObjectiveHeight", stormCompassHeight / 1.66f, "compassObjectiveWidth", stormCompassWidth / 1.66f);
            //players.SetClientDvars("compassObjectiveHeight", 0, "compassObjectiveWidth", 0);//Disable compass icon
        }
        startStormCountdownTimer(stormTime / 1000);
        AfterDelay(stormTime, startStormTimer);
    }
    private static void startStormCountdownTimer(float time, string text = "")
    {
        if (!gameHasStarted)
            return;

        stormCounter.SetTimer(time);
        stormCounter.Color = new Vector3(1, 1, 1);
        if (text == "") stormIcon.SetText("Circle moving in:");
        else stormIcon.SetText(text);
    }
    private static void startStormTimer()
    {
        if (!gameHasStarted) return;

        stormMoving = true;
        OnInterval(1000, stormTimer_update);
        stormCounter.SetTimer(stormTime / 1000);
        stormCounter.Color = new Vector3(.9f, .1f, .1f);
        stormIcon.SetText("Circle moving:");
        PlaySoundAtPos(Vector3.Zero, "plr_new_rank");
        //Announcement("Move fast, the circle is starting to close!");
        AfterDelay(1500, playCircleStartDialogue);
        stormIconLerper.MoveTo(stormIconLerper.Origin - new Vector3(50, 0, 0), stormTime / 1000);
        AfterDelay(stormTime, initNextStormCircle);
    }
    private static bool stormTimer_update()
    {
        if (stormIconLerper.Origin.X > 0)
        {
            stormCompassHeight = (int)stormIconLerper.Origin.X;
            stormCompassWidth = (int)stormIconLerper.Origin.X;
            foreach (Entity players in Players)
            {
                if (!players.IsPlayer) continue;

                if (stormCompassHeight + largeCompassSizeOffset > 25) players.SetClientDvars("compassObjectiveIconHeight", stormCompassHeight + largeCompassSizeOffset, "compassObjectiveIconWidth", stormCompassWidth + largeCompassSizeOffset);
                else players.SetClientDvars("compassObjectiveIconHeight", stormCompassHeight, "compassObjectiveIconWidth", stormCompassWidth);
                //players.SetClientDvars("compassObjectiveHeight", stormCompassHeight / 1.66f, "compassObjectiveWidth", stormCompassWidth / 1.66f);
                //players.SetClientDvars("compassObjectiveHeight", 0, "compassObjectiveWidth", 0);//Disable compass icons
            }
        }
        else return false;

        if (stormTriggerWidth == (int)(stormCompassWidth * 7.5f))
        {
            if (!stormMoving) return false;
            return true;
        }

        //Utilities.PrintToConsole("Updating trigger");
        Vector3 origin = stormTrigger.Origin;
        stormTrigger.Delete();
        if (stormCompassWidth > 0)
        {
            stormTriggerWidth = (int)(stormCompassWidth * 7.5f);
            stormTrigger = Spawn("trigger_radius", origin, 0, stormTriggerWidth, 10000);

            updateStormFX();
        }
        else if (stormCompassWidth == 0)
            stormTrigger = null;

        if (!stormMoving) return false;
        return true;
    }
    private static void updateStormFX(bool instant = true)
    {
        foreach (Entity fx in stormWallFxPoints)
            fx.Delete();//Remove all current points
        stormWallFxPoints.Clear();

        int distance = stormTriggerWidth;//Disance is equal to trigger radius

        int additionalPointScalar = (distance / 150);
        int fxPointCount = minimumStormFxPoints + (2 * additionalPointScalar);

        Vector3 circleOrigin = new Vector3(stormTrigger.Origin.X, stormTrigger.Origin.Y, stormWallFxHeight);
        int offset = 360 / fxPointCount;

        bool extraFx = false;//maxHeight - stormWallFxHeight > 300;

        //Utilities.PrintToConsole("Using " + fxPointCount + " fx points for circle");

        for (int i = 0; i < fxPointCount; i++)
        {
            Vector3 currentAngles = new Vector3(0, offset * i, 0);
            Vector3 currentForward = AnglesToForward(currentAngles);
            Vector3 fxOrigin = circleOrigin + (currentForward * (distance + 200));
            Entity fxEnt = SpawnFX(fx_circleWall, fxOrigin);
            if (instant) TriggerFX(fxEnt, -5);
            else TriggerFX(fxEnt);

            if (extraFx)
            {
                Entity fx2Ent = SpawnFX(fx_circleWall, fxOrigin + new Vector3(0, 0, 150));
                if (instant) TriggerFX(fx2Ent, -5);
                else TriggerFX(fx2Ent);
                stormWallFxPoints.Add(fx2Ent);
            }

            stormWallFxPoints.Add(fxEnt);
        }
    }
    private static void initNextStormCircle()
    {
        if (!gameHasStarted)
            return;

        if (firstCircle) return;
        PlaySoundAtPos(Vector3.Zero, "mp_enemy_obj_returned");
        //Announcement("The circle is done closing... for now.");
        AfterDelay(1500, playCircleStopDialogue);
        stormMoving = false;
        circleShrinkCount++;
        //stormTimer.Interval *= 0.8f;
        setNextStormPosition();

        if (circleShrinkCount == 1 && RandomInt(2) == 1)
            AfterDelay(RandomInt(30000), () => StartAsync(launchAirdrop()));
    }
    private static void initlootLocations()
    {
        string map = GetDvar("mapname");
        switch (map)
        {
            case "mp_dome":
                lootLocations.Add(new Vector3(97, 898, -240));
                lootLocations.Add(new Vector3(-226, 1464, -231));
                lootLocations.Add(new Vector3(-603, 194, -358));
                lootLocations.Add(new Vector3(814, -406, -335));
                lootLocations.Add(new Vector3(5, 1975, -231));
                lootLocations.Add(new Vector3(-673, 1100, -284));
                lootLocations.Add(new Vector3(669, 1028, -255));
                lootLocations.Add(new Vector3(1231, 807, -267));
                lootLocations.Add(new Vector3(709, 210, -342));
                lootLocations.Add(new Vector3(1223, 10, -336));
                lootLocations.Add(new Vector3(-222, 418, -333));
                lootLocations.Add(new Vector3(501, -183, -330));
                break;
            case "mp_plaza2":
                lootLocations.Add(new Vector3(221, 440, 754));
                lootLocations.Add(new Vector3(155, 1763, 668));
                lootLocations.Add(new Vector3(-430, 1871, 691));
                lootLocations.Add(new Vector3(-1190, 1759, 668));
                lootLocations.Add(new Vector3(-1273, 1279, 829));
                lootLocations.Add(new Vector3(-593, 1274, 676));
                lootLocations.Add(new Vector3(-251, 1006, 722));
                lootLocations.Add(new Vector3(80, 1343, 676));
                lootLocations.Add(new Vector3(397, -99, 708));
                lootLocations.Add(new Vector3(-1109, 92, 741));
                lootLocations.Add(new Vector3(-280, -195, 700));
                lootLocations.Add(new Vector3(28, -1600, 668));
                lootLocations.Add(new Vector3(764, -1752, 669));
                break;
            case "mp_mogadishu":
                lootLocations.Add(new Vector3(1448, 1945, 39));
                lootLocations.Add(new Vector3(1499, -1193, 15));
                lootLocations.Add(new Vector3(791, -880, 16));
                lootLocations.Add(new Vector3(38, -1007, 16));
                lootLocations.Add(new Vector3(-691, -260, 22));
                lootLocations.Add(new Vector3(2, 52, 2));
                lootLocations.Add(new Vector3(664, 69, 12));
                lootLocations.Add(new Vector3(1676, 251, -1));
                lootLocations.Add(new Vector3(2314, 1860, 63));
                lootLocations.Add(new Vector3(73, 858, 3));
                lootLocations.Add(new Vector3(710, 837, 16));
                lootLocations.Add(new Vector3(-549, 829, 2));
                lootLocations.Add(new Vector3(34, 1850, 84));
                lootLocations.Add(new Vector3(-778, 2614, 157));
                lootLocations.Add(new Vector3(-204, 3206, 152));
                lootLocations.Add(new Vector3(752, 3189, 148));
                lootLocations.Add(new Vector3(692, 2354, 95));
                break;
            case "mp_paris":
                lootLocations.Add(new Vector3(-931, -921, 110));
                lootLocations.Add(new Vector3(1597, 1768, 47));
                lootLocations.Add(new Vector3(716, 1809, 33));
                lootLocations.Add(new Vector3(258, 2074, 36));
                lootLocations.Add(new Vector3(459, 1067, 37));
                lootLocations.Add(new Vector3(852, 1350, 118));
                lootLocations.Add(new Vector3(1601, 897, 45));
                lootLocations.Add(new Vector3(1286, 420, 41));
                lootLocations.Add(new Vector3(1613, 181, 172));
                lootLocations.Add(new Vector3(466, -752, 67));
                lootLocations.Add(new Vector3(994, -625, 50));
                lootLocations.Add(new Vector3(-211, -60, 63));
                lootLocations.Add(new Vector3(-742, 177, 133));
                lootLocations.Add(new Vector3(-1532, 100, 250));
                lootLocations.Add(new Vector3(-343, 1922, 121));
                lootLocations.Add(new Vector3(-1127, 1555, 284));
                lootLocations.Add(new Vector3(-2025, 1327, 316));
                lootLocations.Add(new Vector3(-1039, 841, 187));
                break;
            case "mp_exchange":
                lootLocations.Add(new Vector3(-614, 1286, 113));
                lootLocations.Add(new Vector3(182, 1155, 148));
                lootLocations.Add(new Vector3(1018, 1254, 120));
                lootLocations.Add(new Vector3(2182, 1322, 145));
                lootLocations.Add(new Vector3(655, 815, 13));
                lootLocations.Add(new Vector3(761, -312, -18));
                lootLocations.Add(new Vector3(761, -771, 112));
                lootLocations.Add(new Vector3(635, -1450, 110));
                lootLocations.Add(new Vector3(152, -1538, 96));
                lootLocations.Add(new Vector3(303, -824, 88));
                lootLocations.Add(new Vector3(-953, -768, 45));
                lootLocations.Add(new Vector3(2392, 1305, 144));
                lootLocations.Add(new Vector3(1634, 1329, 151));
                lootLocations.Add(new Vector3(1315, 743, 159));
                break;
            case "mp_hardhat":
                lootLocations.Add(new Vector3(2035, -229, 246));
                lootLocations.Add(new Vector3(1959, -772, 352));
                lootLocations.Add(new Vector3(1883, -1384, 351));
                lootLocations.Add(new Vector3(848, -1520, 334));
                lootLocations.Add(new Vector3(1326, -1380, 342));
                lootLocations.Add(new Vector3(-338, -1273, 348));
                lootLocations.Add(new Vector3(-821, -884, 348));
                lootLocations.Add(new Vector3(-920, -290, 230));
                lootLocations.Add(new Vector3(-463, -250, 333));
                lootLocations.Add(new Vector3(-741, 208, 245));
                lootLocations.Add(new Vector3(-201, 806, 437));
                lootLocations.Add(new Vector3(224, 980, 436));
                lootLocations.Add(new Vector3(1125, 656, 255));
                lootLocations.Add(new Vector3(1531, 1241, 364));
                lootLocations.Add(new Vector3(1522, 542, 244));
                break;
            case "mp_lambeth":
                lootLocations.Add(new Vector3(-293, -1286, -180));
                lootLocations.Add(new Vector3(-938, -785, -130));
                lootLocations.Add(new Vector3(-375, -250, -187));
                lootLocations.Add(new Vector3(-355, 409, -196));
                lootLocations.Add(new Vector3(161, -5, -181));
                lootLocations.Add(new Vector3(682, -407, -197));
                lootLocations.Add(new Vector3(694, 263, -196));
                lootLocations.Add(new Vector3(690, 1158, -243));
                lootLocations.Add(new Vector3(1181, 801, -67));
                lootLocations.Add(new Vector3(1281, 1248, -257));
                lootLocations.Add(new Vector3(2057, 757, -249));
                lootLocations.Add(new Vector3(1470, -1040, -109));
                lootLocations.Add(new Vector3(1761, -258, -210));
                lootLocations.Add(new Vector3(2800, -652, -186));
                lootLocations.Add(new Vector3(2785, 445, -244));
                lootLocations.Add(new Vector3(2751, 1090, -263));
                lootLocations.Add(new Vector3(1535, 1980, -214));
                lootLocations.Add(new Vector3(1262, 2602, -213));
                lootLocations.Add(new Vector3(419, 2218, -183));
                lootLocations.Add(new Vector3(170, 1631, -182));
                lootLocations.Add(new Vector3(-606, 1549, -201));
                lootLocations.Add(new Vector3(-1199, 1030, -196));
                break;
            case "mp_radar":
                lootLocations.Add(new Vector3(-3482, -498, 1222));
                lootLocations.Add(new Vector3(-4263, -124, 1229));
                lootLocations.Add(new Vector3(-4006, 827, 1238));
                lootLocations.Add(new Vector3(-3375, 342, 1222));
                lootLocations.Add(new Vector3(-4623, 531, 1298));
                lootLocations.Add(new Vector3(-5157, 877, 1200));
                lootLocations.Add(new Vector3(-5950, 1071, 1305));
                lootLocations.Add(new Vector3(-6509, 1660, 1299));
                lootLocations.Add(new Vector3(-7013, 2955, 1359));
                lootLocations.Add(new Vector3(-6333, 3473, 1421));
                lootLocations.Add(new Vector3(-5675, 2923, 1388));
                lootLocations.Add(new Vector3(-7119, 4357, 1380));
                lootLocations.Add(new Vector3(-5487, 4077, 1356));
                lootLocations.Add(new Vector3(-5736, 2960, 1407));
                lootLocations.Add(new Vector3(-4908, 3281, 1225));
                lootLocations.Add(new Vector3(-4421, 4071, 1268));
                lootLocations.Add(new Vector3(-4979, 1816, 1205));
                lootLocations.Add(new Vector3(-4874, 2306, 1223));
                break;
        }
    }
    private static IEnumerator launchAirdrop()
    {
        PlaySoundAtPos(Vector3.Zero, gameDialogue[announcer + "_airdropDialogue"][0]);//Incoming

        yield return Wait(2);

        int randomSpawn = RandomInt(lootLocations.Count);
        Vector3 location = lootLocations[randomSpawn];

        Entity heightEnt = GetEnt("airstrikeheight", "targetname");
        float height = location.Z + 4000;
        if (heightEnt != null) height = heightEnt.Origin.Z;
        Vector3 spawn = new Vector3(location.X, location.Y, height);
        spawn = spawn.Around(400);

        Entity package = Spawn("script_model", spawn);
        package.Angles = new Vector3(RandomInt(360), RandomInt(360), RandomInt(360));
        package.SetModel("com_plasticcase_friendly");
        package.CloneBrushModelToScriptModel(_airdropCollision);
        package.SetField("isAirdrop", true);
        package.MoveTo(location, 4);
        Vector3 angles = VectorToAngles(spawn - location);
        //Vector3 forward = AnglesToForward(angles);
        //Vector3 right = AnglesToRight(angles);
        //Entity fx = SpawnFX(fx_airdropThruster, package.Origin, forward, right);
        //fx.LinkTo(package, "tag_origin", Vector3.Zero, Vector3.Zero);
        //TriggerFX(fx);
        Entity fx = Spawn("script_model", package.Origin);
        fx.Angles = angles;
        fx.SetModel("tag_origin");
        fx.LinkTo(package);
        AfterDelay(100, () => PlayFXOnTag(fx_airdropThruster, fx, "tag_origin"));
        package.PlayLoopSound("move_40mm_proj_loop1");

        yield return Wait(4);

        package.StopLoopSound();
        package.PhysicsLaunchServer(Vector3.Zero, Vector3.Zero);
        package.PlaySound("exp_ac130_40mm");

        fx.Unlink();
        StopFXOnTag(fx_airdropThruster, fx, "tag_origin");
        fx.Delete();

        PlayFX(fx_carePackageImpact, location);
        RadiusDamage(location, 64, 500, 500, Entity.GetEntity(2046), "MOD_CRUSH", "");
        usables.Add(package);

        yield return Wait(1);

        PlaySoundAtPos(Vector3.Zero, gameDialogue[announcer + "_airdropDialogue"][1]);
    }

    private static void onGlobalNotify(int entRef, string message, params Parameter[] parameters)
    {
        if (message == "fontPulse" && !rankingStarted)
        {
            HudElem countdownTimer = HudElem.GetHudElem(entRef);
            HudElem countdownText = HudElem.GetHudElem(entRef - 1);

            if (!gameHasStarted)
            {
                //countdownText.SetText("Match starting in:");
                countdownText.HorzAlign = HudElem.HorzAlignments.Left_Adjustable;
                countdownText.VertAlign = HudElem.VertAlignments.Top_Adjustable;
                countdownText.X = 110;
                countdownText.Y = 10;
                countdownText.FontScale = 1.3f;
                countdownTimer.HorzAlign = HudElem.HorzAlignments.Left_Adjustable;
                countdownTimer.VertAlign = HudElem.VertAlignments.Top_Adjustable;
                countdownTimer.X = 64;
                countdownTimer.Y = 30;
            }
            else
            {
                countdownText.HorzAlign = HudElem.HorzAlignments.Center_Adjustable;
                countdownText.VertAlign = HudElem.VertAlignments.Center_Adjustable;
                countdownText.X = 0;
                countdownText.Y = 0;
                countdownText.FontScale = 1.5f;
                countdownTimer.HorzAlign = HudElem.HorzAlignments.Center_Adjustable;
                countdownTimer.VertAlign = HudElem.VertAlignments.Center_Adjustable;
                countdownTimer.X = 0;
                countdownTimer.Y = 20;
                PlaySoundAtPos(Vector3.Zero, "mp_bonus_end");
            }
        }
        if (message == "match_start_timer_beginning")
        {
            if (firstCircle)
            {
                firstCircle = false;
                return;
            }
            gameHasStarted = true;

            startHint.Destroy();

            playerClone.MoveTo(playerClone.Origin + new Vector3(0, 0, 100), 4);
            AfterDelay(3000, () =>
            {
                playerClone.GetField<Entity>("head").Delete();
                playerClone.Delete();
            });

            foreach (Entity players in Players)
            {
                if (!players.HasField("hud_created")) continue;
                if (players.Classname != "player" || !players.IsAlive) continue;

                players.EndLocationSelection();
                players.VisionSetNakedForPlayer("", .5f);
                AfterDelay(1000, () => players.VisionSetNakedForPlayer("black_bw", 2));

                players.GetField<Entity>("pedistal").MoveTo(players.GetField<Entity>("pedistal").Origin + new Vector3(0, 0, 100), 4);

                AfterDelay(3000, () =>
                {
                    Objective_Delete(players.EntRef);
                    players.VisionSetNakedForPlayer("", 2.5f);
                    players.CameraUnlink();
                    players.GetField<Entity>("pedistal").GetField<Entity>("camera").Delete();
                    spawnPlayer(players);
                });
            }
        }
        if (message == "prematch_over")
        {
            startStormCountdownTimer(10, "Circle forming in:");
            AfterDelay(10000, () =>
            {
                startStormCountdownTimer(60);
                AfterDelay(60000, () => Notify("start_circle"));
                foreach (Entity players in Players)
                {
                    if (players.IsAlive) OnInterval(1000, () => trackCirclePresence(players));
                }
            });
            Objective_Add(stormCircleMapID, "active", deployVector, "compassping_enemy_uav");
            stormTrigger = Spawn("trigger_radius", deployVector - new Vector3(0, 0, 2500), 0, stormTriggerWidth, 10000);
            //AfterDelay(2000, () => Announcement("Solo. A little tip for you contestants: be the last one breathing. Hahahah!"));
            updateStormFX(false);
        }
        if (message == "start_circle")
        {
            //initDeployVectors();
            if (deployVector.Equals(Vector3.Zero))
            {
                Utilities.PrintToConsole("deployVector was not set up when circle was started!");
                return;
            }

            foreach (Entity players in Players)
            {
                if (!players.IsPlayer) continue;

                players.SetClientDvars("compassObjectiveIconHeight", stormCompassHeight, "compassObjectiveIconWidth", stormCompassWidth);
            }
            startStormTimer();
        }

        if (entRef > 18) return;
        Entity player = Entity.GetEntity(entRef);

        if (message == "reload")
        {
            string ammoType = getAmmoType(player.CurrentWeapon);
            player.SetField(ammoType + "Ammo", player.GetWeaponAmmoStock(player.CurrentWeapon));
            updateAllWeaponAmmoCounts(player);
        }

        else if (message == "weapon_switch_started" || message == "weapon_change")
        {
            if (parameters[0].As<string>() == "lightstick_mp")
            {
                if (player.Health == 125)
                {
                    player.SwitchToWeapon(player.GetField<string[]>("weaponsList")[0]);
                    return;
                }
            }
            updateWeaponsHud(player);
        }

        else if (message == "weapon_fired")
        {
            if (parameters[0].As<string>() == "lightstick_mp")
            {
                player.Health += 40;
                if (player.Health > 125) player.Health = 125;
                updatePlayerHealthHud(player);
                int health = player.GetField<int>("healthPacks");
                health--;
                if (health < 0) health = 0;
                player.SetField("healthPacks", health);
                if (health > 0) player.GiveWeapon("lightstick_mp");
                updateWeaponsHud(player);
            }
        }

        else if (message == "confirm_location")
        {
            if (player.GetField<bool>("hasClicked") || gameHasStarted) return;

            player.SetField("hasClicked", true);
            if (!player.HasField("compass_circle"))
            {
                Objective_Add(player.EntRef, "active", player.Origin, "compassping_portable_radar_sweep");
                player.SetField("compass_circle", true);
            }
            Vector3 newLocation = parameters[0].As<Vector3>();
            Vector3 oldLocation = player.Origin;
            player.SetOrigin(newLocation + new Vector3(0, 0, 10000));
            Vector3 ground = GetGroundPosition(new Vector3(newLocation.X, newLocation.Y, maxHeight), 20);
            Vector3 trace = PlayerPhysicsTrace(ground + new Vector3(0, 0, 60), ground);
            float traceDistance = trace.DistanceTo(ground);
            if (traceDistance > 1)
            {
                //Retrace lower, maybe maxheight is too high for this location
                ground = GetGroundPosition(new Vector3(newLocation.X, newLocation.Y, maxHeight - 120), 20);
                trace = PlayerPhysicsTrace(ground + new Vector3(0, 0, 60), ground);
                traceDistance = trace.DistanceTo(ground);
                if (traceDistance > 1)
                {
                    //Trace one last time
                    ground = GetGroundPosition(new Vector3(newLocation.X, newLocation.Y, maxHeight - 240), 20);
                    trace = PlayerPhysicsTrace(ground + new Vector3(0, 0, 60), ground);
                    traceDistance = trace.DistanceTo(ground);
                    if (traceDistance > 1)
                    {
                        //player.IPrintLnBold("^1Cannot deploy here!");
                        //player.ClientPrint("MP_REMOTE_TANK_CANNOT_PLACE");
                        displayCannotDeployWarning(player);
                        player.SetOrigin(oldLocation);//Restore location
                        AfterDelay(1000, () => player.SetField("hasClicked", false));
                        return;
                    }
                }
            }
            player.SetField("selectedSpawn", ground);
            Objective_Position(player.EntRef, newLocation + new Vector3(RandomFloatRange(-100, 100), RandomFloatRange(-100, 100), 0));
            AfterDelay(1000, () => player.SetField("hasClicked", false));
        }
        else if (message.StartsWith("use_button_pressed"))
        {
            checkPlayerUsables(player);
        }
        else if (message == "spectating_cycle")
        {
            foreach (Entity alive in playersAlive)
                alive.SetField("spectatingPlayers", 0);//Reset count for all alive players

            foreach (Entity players in Players)//Check who everyone is spectating and increment the spectatee's count
            {
                if (players.SessionState != "spectator") continue;

                Entity spectatee = players.GetSpectatingPlayer();
                if (spectatee == null) continue;

                spectatee.SetField("spectatingPlayers", spectatee.GetField<int>("spectatingPlayers") + 1);
            }

            foreach (Entity alive in playersAlive)//Loop through alive players to update spectating hud
            {
                if (alive.SessionState != "playing") continue;

                int spectating = alive.GetField<int>("spectatingPlayers");
                HudElem spectateCount = alive.GetField<HudElem>("hud_spectateCount");

                if (spectating == 0)
                    spectateCount.SetText("");
                else
                    spectateCount.SetText(createHudShaderString("iw5_cardicon_lightingeye", true) + " ^3" + spectating.ToString());
            }
        }
        else if (message.StartsWith("menuresponse"))//Force class prevention
        {
            if (parameters[0].As<string>().StartsWith("changeclass"))
            {
                Utilities.ExecuteCommand("kickclient " + player.EntRef + " MP_CHANGE_CLASS_NEXT_SPAWN");
            }
            else if ((string)parameters[0] == "team_marinesopfor")
            {
                Utilities.ExecuteCommand("kickclient " + player.EntRef + " MP_CANTJOINTEAM");
            }
        }
        else if (message.StartsWith("-scoreboard:"))
        {
            Entity caller = Entity.GetEntity(int.Parse(message.Split(':')[1]));
            caller.SetField("isViewingScoreboard", false);
        }
        else if (message.StartsWith("+scoreboard:"))
        {
            Entity caller = Entity.GetEntity(int.Parse(message.Split(':')[1]));
            caller.SetField("isViewingScoreboard", true);
        }
    }

    private static void setSpawnModel(Entity player)
    {
        player.SetModel(getPlayerModelsForLevel(false));
        //player.SetViewModel(bodyModel);
        player.Attach(getPlayerModelsForLevel(true), "j_spine4", true);
        player.ShowPart("j_spine4", getPlayerModelsForLevel(true));
        //player.Show();
    }

    public override void OnPlayerKilled(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
    {
        recordPlayerAmmoValues(player);
        player.TakeAllWeapons();//Temp to replace SetDropItemEnabled(false);
        AfterDelay(0, () =>
        {
            dropAllPlayerWeapons(player);
            dropAllPlayerAmmo(player);
            dropAllPlayerHealth(player);
            dropAllPlayerPerks(player);
            dropAllPlayerAttachments(player);
            clearPlayerWeaponsList(player);

            displayDamageDone(player, attacker);

            int rank = getPlayerRank(player);
            player.SetField("rank", rank);
            player.IPrintLn(string.Format("You placed {0}!", rank));
            Announcement(string.Format("{0} was eliminated!", player.Name));
            //PlaySoundAtPos(Vector3.Zero, "mp_war_objective_taken");
            player.PlaySound("trophy_fire");

            if (playersAlive.Contains(player)) playersAlive.Remove(player);
            updatePlayersAliveCount();

            AfterDelay(500, () =>
            {
                if (playersAlive.Count < 2)
                {
                    endGame();
                }
                else if (playersAlive.Count == 2)
                {
                    AfterDelay(4500, playLastAliveDialogue);
                }
                AfterDelay(500, () =>
                {
                    player.SessionState = "spectator";
                    player.SessionTeam = "spectator";
                    playLoseDialogue(player);
                    if (playersAlive.Count > 1) playKillDialogue();

                    if (player.GetField<int>("rank") < 6 && player.GetField<int>("rank") != -1)
                        player.StatusIcon = icons_rank[player.GetField<int>("rank") - 1];
                });
            });
        });
    }
    public override void OnPlayerDamage(Entity player, Entity inflictor, Entity attacker, int damage, int dFlags, string mod, string weapon, Vector3 point, Vector3 dir, string hitLoc)
    {
        if (mod == "MOD_FALLING")
        {
            AfterDelay(50, () => updatePlayerHealthHud(player));
            return;
        }
        //Tier damages
        if (weapon.EndsWith("_camo01") && mod != "MOD_MELEE")
        {
            damage += 3;
            player.FinishPlayerDamage(inflictor, attacker, 3, dFlags, mod, weapon, point, dir, hitLoc, 0);
        }
        else if (weapon.EndsWith("_camo02") && mod != "MOD_MELEE")
        {
            damage += 6;
            player.FinishPlayerDamage(inflictor, attacker, 6, dFlags, mod, weapon, point, dir, hitLoc, 0);
        }
        else if (weapon.EndsWith("_camo03") && mod != "MOD_MELEE")
        {
            damage += 9;
            player.FinishPlayerDamage(inflictor, attacker, 9, dFlags, mod, weapon, point, dir, hitLoc, 0);
        }
        else if (weapon.EndsWith("_camo04") && mod != "MOD_MELEE")
        {
            damage += 12;
            player.FinishPlayerDamage(inflictor, attacker, 12, dFlags, mod, weapon, point, dir, hitLoc, 0);
        }

        Dictionary<Entity, int> attackers = player.GetField<Dictionary<Entity, int>>("attackers");
        if (!attackers.ContainsKey(attacker)) attackers.Add(attacker, 0);

        attackers[attacker] += damage;

        //Utilities.PrintToConsole("Recording a hit on " + player.Name + " of " + damage + " with a remaining health of " + player.Health);

        AfterDelay(0, () =>
        {
            if (player.GetField<int>("shield") > 0)
            {
                int shieldDamage = damage - (damage / 4);
                player.Health += shieldDamage;
                if (player.Health > 125) player.Health = 125;

                int newShield = player.GetField<int>("shield") - shieldDamage;
                player.SetField("shield", Math.Max(0, newShield));
                if (newShield < 0 && player.IsAlive) player.FinishPlayerDamage(inflictor, attacker, Math.Abs(newShield), dFlags, mod, weapon, point, dir, hitLoc, 0);
            }
            updatePlayerHealthHud(player);

            if (player.Health < 0)
            {
                attackers[attacker] += player.Health;//Health is negative so add it back
                player.Suicide();
            }
        });
    }

    private static void endGame()
    {
        if (!gameHasStarted || firstCircle || !rankingStarted || playersAlive.Count > 1)
            return;

        hideGameHud();
        foreach(Entity player in Players)
        {
            if (player.Classname != "player")
                continue;

            //Reset dvars in case anyone quits post-game
            player.SetClientDvar("compassRotation", true);
            player.SetClientDvar("hud_enable", 1);

            hidePlayerHud(player);
        }

        Entity lastPlayer = null;
        if (playersAlive.Count > 0) lastPlayer = playersAlive[0];

        if (lastPlayer != null)
        {
            //lastPlayer.Score = 10000;
            lastPlayer.StatusIcon = icons_rank[0];
            SetWinningPlayer(lastPlayer);
            //SetDynamicDvar("scr_dm_timelimit", 0.5f);
            lastPlayer.Notify("menuresponse", "win", "endround");
        }
        else SetDynamicDvar("scr_dm_timelimit", 0.5f);//If both end players die at the same time, this will trigger
        playWinDialogue();
        gameHasStarted = false;
    }

    public static int getPlayerRank(Entity player)
    {
        int rank = 0;
        foreach (Entity players in Players)
        {
            if (players == player) continue;
            if (players.IsAlive)
                rank++;
        }
        return rank + 1;
    }

    public static void createServerHud()
    {
        stormCounter = NewHudElem();
        stormCounter.X = 105;
        stormCounter.Y = 120;
        stormCounter.AlignX = HudElem.XAlignments.Left;
        stormCounter.AlignY = HudElem.YAlignments.Top;
        stormCounter.HorzAlign = HudElem.HorzAlignments.Left_Adjustable;
        stormCounter.VertAlign = HudElem.VertAlignments.Top_Adjustable;
        stormCounter.Foreground = true;
        stormCounter.Alpha = 1;
        stormCounter.Archived = true;
        stormCounter.HideWhenInMenu = true;
        stormCounter.Font = HudElem.Fonts.Default;
        stormCounter.FontScale = 1.5f;

        stormIcon = NewHudElem();
        stormIcon.X = 5;
        stormIcon.Y = 120;
        stormIcon.AlignX = HudElem.XAlignments.Left;
        stormIcon.AlignY = HudElem.YAlignments.Top;
        stormIcon.HorzAlign = HudElem.HorzAlignments.Left_Adjustable;
        stormIcon.VertAlign = HudElem.VertAlignments.Top_Adjustable;
        stormIcon.Foreground = true;
        stormIcon.Alpha = 1;
        stormIcon.Archived = true;
        stormIcon.HideWhenInMenu = true;
        stormIcon.Font = HudElem.Fonts.Default;
        stormIcon.FontScale = 1.5f;
        //stormIcon.SetText("Storm moving in: ");

        playerCount = NewHudElem();
        playerCount.X = 5;
        playerCount.Y = -45;
        playerCount.AlignX = HudElem.XAlignments.Left;
        playerCount.AlignY = HudElem.YAlignments.Bottom;
        playerCount.HorzAlign = HudElem.HorzAlignments.Left_Adjustable;
        playerCount.VertAlign = HudElem.VertAlignments.Bottom_Adjustable;
        playerCount.Foreground = true;
        playerCount.Alpha = 1;
        playerCount.Archived = true;
        playerCount.HideWhenInMenu = true;
        playerCount.Font = HudElem.Fonts.HudBig;
        playerCount.FontScale = 1.5f;

        startHint = NewHudElem();
        startHint.X = 0;
        startHint.Y = -35;
        startHint.AlignX = HudElem.XAlignments.Center;
        startHint.AlignY = HudElem.YAlignments.Bottom;
        startHint.HorzAlign = HudElem.HorzAlignments.Center_Adjustable;
        startHint.VertAlign = HudElem.VertAlignments.Bottom_Adjustable;
        startHint.Foreground = true;
        startHint.Alpha = 1;
        startHint.Archived = false;
        startHint.HideWhenInMenu = false;
        startHint.Font = HudElem.Fonts.Default;
        startHint.FontScale = 2f;
        startHint.SetText("Select your starting position.");
    }

    private static void hideGameHud()
    {
        stormCounter.Alpha = 0;
        stormIcon.Alpha = 0;
        playerCount.Alpha = 0;
    }

    public static void createPlayerHud(Entity player)
    {
        if (player.HasField("hud_created")) return;

        //weapon rarity
        HudElem rarity = HudElem.CreateIcon(player, "line_horizontal", 196, 48);
        rarity.SetPoint("bottom right", "bottom right", -45, 0);
        rarity.HideWhenInMenu = true;
        rarity.HideWhenDead = true;
        rarity.Foreground = false;
        rarity.Archived = true;
        rarity.Color = new Vector3(.3f, .3f, .3f);
        rarity.Alpha = 0.4f;
        player.SetField("hud_rarity", rarity);

        //health hud
        HudElem health = HudElem.CreateIcon(player, "progress_bar_fill", 100, 10);
        health.SetPoint("bottom", "bottom", -50, -10);
        health.AlignX = HudElem.XAlignments.Left;
        health.HideWhenInMenu = true;
        health.Foreground = false;
        health.Archived = true;
        health.HideWhenDead = true;
        health.Alpha = 0;
        health.Color = new Vector3(0, 0.7f, 0);
        health.Sort = 10;
        /*
        HudElem healthBG = HudElem.CreateIcon(player, "progress_bar_fill", 100, 10);
        healthBG.Parent = health;
        healthBG.SetPoint("center");
        healthBG.HideWhenInMenu = true;
        healthBG.Foreground = false;
        healthBG.Archived = true;
        healthBG.Alpha = .7f;
        healthBG.Color = new Vector3(0, 0, 0);
        healthBG.Sort = 10;
        */
        HudElem healthNumber = HudElem.CreateFontString(player, HudElem.Fonts.Default, 1);
        healthNumber.Parent = health;
        healthNumber.SetPoint("right", "right", 17, 0);
        healthNumber.HideWhenInMenu = true;
        healthNumber.Foreground = true;
        healthNumber.Archived = true;
        healthNumber.HideWhenDead = true;
        healthNumber.Alpha = 0;
        healthNumber.SetValue(125);
        healthNumber.Sort = 10;
        HudElem healthMax = HudElem.CreateFontString(player, HudElem.Fonts.Default, 1);
        healthMax.Parent = healthNumber;
        healthMax.SetPoint("left", "left", 3);
        healthMax.HideWhenInMenu = true;
        healthMax.Foreground = true;
        healthMax.Archived = true;
        healthMax.HideWhenDead = true;
        healthMax.Alpha = 0;
        healthMax.SetText("/125");
        healthMax.Sort = 10;
        health.SetField("percent", Math.Min(player.Health, 125));

        HudElem shield = HudElem.CreateIcon(player, "progress_bar_fill", 0, 10);
        shield.SetPoint("bottom", "bottom", -50, -25);
        shield.AlignX = HudElem.XAlignments.Left;
        shield.HideWhenInMenu = true;
        shield.Foreground = false;
        shield.Archived = true;
        shield.HideWhenDead = true;
        shield.Alpha = 0;
        shield.Color = new Vector3(0, 0.4f, 0.7f);
        shield.Sort = 10;
        /*
        HudElem shieldBG = HudElem.CreateIcon(player, "progress_bar_fill", 100, 10);
        shieldBG.Parent = shield;
        shieldBG.SetPoint("center");
        shieldBG.HideWhenInMenu = true;
        shieldBG.Foreground = false;
        shieldBG.Archived = true;
        shieldBG.Alpha = .7f;
        shieldBG.Color = new Vector3(0, 0, 0);
        shieldBG.Sort = 10;
        */
        HudElem shieldNumber = HudElem.CreateFontString(player, HudElem.Fonts.Default, 1);
        shieldNumber.Parent = shield;
        shieldNumber.SetPoint("right", "right", 117, 0);
        shieldNumber.HideWhenInMenu = true;
        shieldNumber.Foreground = true;
        shieldNumber.Archived = true;
        shieldNumber.HideWhenDead = true;
        shieldNumber.Alpha = 0;
        shieldNumber.SetValue(player.GetField<int>("shield"));
        shieldNumber.Sort = 10;
        HudElem shieldMax = HudElem.CreateFontString(player, HudElem.Fonts.Default, 1);
        shieldMax.Parent = shieldNumber;
        shieldMax.SetPoint("left", "left", 3);
        shieldMax.HideWhenInMenu = true;
        shieldMax.Foreground = true;
        shieldMax.Archived = true;
        shieldMax.HideWhenDead = true;
        shieldMax.Alpha = 0;
        shieldMax.SetText("/200");
        shieldMax.Sort = 10;
        shield.SetField("percent", 0);

        player.SetField("hud_health", health);
        player.SetField("hud_healthNumber", healthNumber);
        player.SetField("hud_shield", shield);
        player.SetField("hud_shieldNumber", shieldNumber);

        HudElem perks = HudElem.CreateFontString(player, HudElem.Fonts.HudBig, 1);
        perks.SetPoint("bottom right", "bottom right", -325, -15);
        perks.AlignX = HudElem.XAlignments.Left;
        perks.HideWhenInMenu = true;
        perks.Foreground = true;
        perks.Archived = true;
        perks.HideWhenDead = true;
        perks.Alpha = 1;
        perks.SetText("");
        perks.SetField("text", "");
        perks.Sort = 10;

        player.SetField("hud_perks", perks);

        //usables message
        /*
        HudElem message = HudElem.CreateFontString(player, HudElem.Fonts.Default, 1.6f);
        message.SetPoint("CENTER", "CENTER", 0, 110);
        message.HideWhenInMenu = true;
        message.HideWhenDead = true;
        //message.Foreground = true;
        message.Alpha = 0;
        message.Archived = true;
        message.Sort = 20;
        player.SetField("hud_message", message);
        */
        //usables waypoint hud
        /*
        HudElem message_world = HudElem.CreateFontString(player, HudElem.Fonts.Default, 1.6f);
        message_world.HideWhenInMenu = true;
        message_world.HideWhenDead = true;
        //message.Foreground = true;
        message_world.Alpha = 0;
        message_world.Archived = true;
        message_world.Sort = 20;
        player.SetField("hud_message_world", message_world);
        */

        HudElem spectateCount = HudElem.CreateFontString(player, HudElem.Fonts.HudSmall, 1);
        spectateCount.AlignX = HudElem.XAlignments.Right;
        spectateCount.AlignY = HudElem.YAlignments.Top;
        spectateCount.HorzAlign = HudElem.HorzAlignments.Right_Adjustable;
        spectateCount.VertAlign = HudElem.VertAlignments.Top_Adjustable;
        spectateCount.X = 150;
        spectateCount.Y = 10;
        spectateCount.HideWhenInMenu = true;
        spectateCount.Foreground = true;
        spectateCount.Archived = true;
        spectateCount.HideWhenDead = true;
        spectateCount.Alpha = 1;
        spectateCount.SetText("");
        spectateCount.Sort = 20;
        player.SetField("hud_spectateCount", spectateCount);

        player.SetField("hud_created", true);
    }
    public static string createHudShaderString(string shader, bool flipped = false, int width = 64, int height = 64)
    {
        byte[] str;
        byte flip;
        flip = (byte)(flipped ? 2 : 1);
        byte w = (byte)width;
        byte h = (byte)height;
        byte length = (byte)shader.Length;
        str = new byte[4] { flip, w, h, length };
        string ret = "^" + Encoding.UTF8.GetString(str);
        return ret + shader;
    }
    public static HudElem createPrimaryProgressBar(Entity player, string text = "")
    {
        HudElem progressBar = HudElem.CreateIcon(player, "progress_bar_fill", 0, 9);//NewClientHudElem(player);
        progressBar.SetField("frac", 0);
        progressBar.Color = new Vector3(1, 1, 1);
        progressBar.Sort = -2;
        progressBar.Shader = "progress_bar_fill";
        progressBar.SetShader("progress_bar_fill", 1, 9);
        progressBar.Alpha = 1;
        progressBar.SetPoint("center", "", 0, -61);
        progressBar.AlignX = HudElem.XAlignments.Left;
        progressBar.X = -60;

        HudElem progressBarBG = HudElem.CreateIcon(player, "progress_bar_bg", 124, 13);//NewClientHudElem(player);
        progressBarBG.SetPoint("center", "", 0, -61);
        progressBarBG.SetField("bar", progressBar);
        progressBarBG.Sort = -3;
        progressBarBG.Color = new Vector3(0, 0, 0);
        progressBarBG.Alpha = .5f;

        if (text != "")
        {
            HudElem progressBarText = HudElem.CreateFontString(player, HudElem.Fonts.HudBig, .6f);//NewClientHudElem(player);
            progressBarText.Parent = progressBarBG;
            progressBarText.SetPoint("center", "center", 0, -10);
            progressBarText.Sort = -1;
            progressBarText.SetText(text);
        }

        player.SetField("progressBar", progressBarBG);
        return progressBarBG;
    }

    public static void updateBar(HudElem barBG, int barFrac, float rateOfChange)
    {
        HudElem bar = (HudElem)barBG.GetField("bar");
        bar.SetField("frac", barFrac);

        if (rateOfChange > 0)
            bar.ScaleOverTime(rateOfChange, barFrac, bar.Height);
        else if (rateOfChange < 0)
            bar.ScaleOverTime(-1 * rateOfChange, barFrac, bar.Height);
    }
    public static void destroyPrimaryProgressBar(HudElem barBG)
    {
        HudElem bar = (HudElem)barBG.GetField("bar");
        HudElem text = null;
        if (barBG.Children.Count > 0)
        {
            text = barBG.Children[0];
        }

        bar.Destroy();
        if (text != null) text.Destroy();
        barBG.Destroy();
    }
    public static void destroyPlayerHud(Entity player)
    {
        if (!player.HasField("hud_created")) return;
        HudElem[] HUD = new HudElem[6] {
                player.GetField<HudElem>("hud_rarity"),
                player.GetField<HudElem>("hud_health"),
                player.GetField<HudElem>("hud_healthNumber"),
                player.GetField<HudElem>("hud_shield"),
                player.GetField<HudElem>("hud_shieldNumber"),
                player.GetField<HudElem>("hud_perks"),
                //player.GetField<HudElem>("hud_message_world") 
            };

        HUD[1].Children.ForEach((h) => h.Destroy());
        HUD[2].Children.ForEach((h) => h.Destroy());
        HUD[3].Children.ForEach((h) => h.Destroy());
        HUD[4].Children.ForEach((h) => h.Destroy());

        foreach (HudElem hud in HUD)
        {
            //hud.Reset();
            if (hud == null) continue;
            hud.Destroy();
        }

        player.ClearField("hud_rarity");
        player.ClearField("hud_health");
        player.ClearField("hud_healthNumber");
        player.ClearField("hud_shield");
        player.ClearField("hud_shieldNumber");
        //player.ClearField("hud_message");
        //player.ClearField("hud_message_world");
        player.ClearField("hud_perks");
        player.ClearField("hud_created");
    }
    public static void hidePlayerHud(Entity player)
    {
        if (!player.HasField("hud_created")) return;

        HudElem[] HUD = new HudElem[6] {
                player.GetField<HudElem>("hud_rarity"),
                player.GetField<HudElem>("hud_health"),
                player.GetField<HudElem>("hud_healthNumber"),
                player.GetField<HudElem>("hud_shield"),
                player.GetField<HudElem>("hud_shieldNumber"),
                player.GetField<HudElem>("hud_perks"),
                //player.GetField<HudElem>("hud_message_world") 
            };

        HUD[1].Children.ForEach((h) => h.Alpha = 0);
        HUD[2].Children.ForEach((h) => h.Alpha = 0);
        HUD[3].Children.ForEach((h) => h.Alpha = 0);
        HUD[4].Children.ForEach((h) => h.Alpha = 0);

        foreach (HudElem hud in HUD)
        {
            //hud.Reset();
            if (hud == null) continue;
            hud.Alpha = 0;
        }
    }
    private static void hudPickup(Entity player, string message)
    {
        HudElem popup = NewClientHudElem(player);//HudElem.CreateFontString(player, HudElem.Fonts.HudSmall, 0.8f);
        popup.HorzAlign = HudElem.HorzAlignments.Center;
        popup.VertAlign = HudElem.VertAlignments.Middle;
        popup.AlignX = HudElem.XAlignments.Center;
        popup.AlignY = HudElem.YAlignments.Middle;
        popup.X = 0;
        popup.Y = 0;
        popup.Font = HudElem.Fonts.HudSmall;
        popup.FontScale = 0.8f;
        popup.Alpha = 1;
        popup.Archived = false;
        popup.Foreground = true;
        popup.HideWhenDead = true;
        popup.HideWhenInMenu = true;
        popup.LowResBackground = false;
        popup.SetText(message);
        popup.MoveOverTime(1);
        popup.Y -= 100;
        popup.FadeOverTime(1);
        popup.Alpha = 0;
        AfterDelay(1000, () => popup.Destroy());
    }
    private static void displayCannotDeployWarning(Entity player)
    {
        player.PlayLocalSound("ui_text_type");

        HudElem text = HudElem.CreateFontString(player, HudElem.Fonts.BigFixed, 1);
        text.SetPoint("center");
        text.SetText("^1Cannot deploy here!");
        text.Alpha = 1;
        text.Foreground = true;
        text.FadeOverTime(1);
        text.Alpha = 0;
        AfterDelay(1000, () => text.Destroy());
    }
    private static void displayDamageDone(Entity player, Entity attacker)
    {
        if (!player.HasField("attackers")) return;
        Dictionary<Entity, int> attackers = player.GetField<Dictionary<Entity, int>>("attackers");

        if (!attackers.ContainsKey(attacker)) return;

        HudElem text = HudElem.CreateFontString(attacker, HudElem.Fonts.Objective, 2);
        text.AlignX = HudElem.XAlignments.Center;
        text.AlignY = HudElem.YAlignments.Bottom;
        text.HorzAlign = HudElem.HorzAlignments.Center_Adjustable;
        text.VertAlign = HudElem.VertAlignments.Bottom_Adjustable;
        text.X = 0;
        text.Y = -150;
        text.SetText("^1" + attackers[attacker].ToString() + " ^7Damage dealt");
        text.Alpha = 0;
        text.Foreground = true;
        text.FadeOverTime(.25f);
        text.Alpha = 1;
        AfterDelay(3000, () => text.Destroy());
    }
    public static void updateWeaponsHud(Entity player)
    {
        if (!player.HasField("hud_created") || (player.HasField("hud_created") && !player.GetField<bool>("hud_created")))
            return;

        //Update special slots
        if (player.GetField<int>("healthPacks") > 0)
        {
            //player.SetPlayerData("killstreaksState", "icons", 0, 36);
            player.SetPlayerData("killstreaksState", "hasStreak", 1, true);
            player.SetPlayerData("killstreaksState", "selectedIndex", 1);
            player.SetPlayerData("killstreaksState", "count", player.GetField<int>("healthPacks"));
        }
        else
        {
            player.SetPlayerData("killstreaksState", "hasStreak", 1, false);
            player.SetPlayerData("killstreaksState", "count", player.GetField<int>("healthPacks"));
        }

        HudElem rarity = player.GetField<HudElem>("hud_rarity");
        rarity.Color = getWeaponRarityColor(player.CurrentWeapon);

        updateAttachments(player);
    }
    private static void updatePlayerHealthHud(Entity player)
    {
        HudElem health = player.GetField<HudElem>("hud_health");
        HudElem healthNumber = player.GetField<HudElem>("hud_healthNumber");
        HudElem shield = player.GetField<HudElem>("hud_shield");
        HudElem shieldNumber = player.GetField<HudElem>("hud_shieldNumber");

        health.Alpha = 1;
        healthNumber.Alpha = 1;
        healthNumber.Children[0].Alpha = .6f;
        shield.Alpha = 1;
        shieldNumber.Alpha = 1;
        shieldNumber.Children[0].Alpha = .6f;

        int healthPercent = (int)health.GetField("percent");
        int shieldPercent = (int)shield.GetField("percent");
        float newHealthPercent = 100f * (player.Health / 125f);
        float newShieldPercent = 100f * (player.GetField<int>("shield") / 200f);

        //InfinityScript.Log.Debug("Health {0}, newHealth {1}, shield {2}, newShield {3}", healthPercent, newHealthPercent, shieldPercent, newShieldPercent);

        if (healthPercent != newHealthPercent)
        {
            health.ScaleOverTime(.5f, (int)newHealthPercent, health.Height);
            health.SetField("percent", newHealthPercent);
            healthNumber.SetValue(player.Health);
        }
        if (shieldPercent != newShieldPercent)
        {
            shield.ScaleOverTime(.5f, (int)newShieldPercent, shield.Height);
            shield.SetField("percent", newShieldPercent);
            shieldNumber.SetValue(player.GetField<int>("shield"));
        }
    }
    private static void updateAllWeaponAmmoCounts(Entity player)
    {
        foreach (string weapon in player.GetField<string[]>("weaponsList"))
        {
            if (string.IsNullOrEmpty(weapon)) continue;
            if (!weapon.StartsWith("iw5_")) continue;
            string ammoType = getAmmoType(weapon);

            player.SetWeaponAmmoStock(weapon, player.GetField<int>(ammoType + "Ammo"));
        }
    }
    private static void updateAttachments(Entity player)
    {
        string[][] attachmentsList = player.GetField<string[][]>("attachmentsList");
        string[] weaponsList = player.GetField<string[]>("weaponsList");
        string currentWeapon = player.CurrentWeapon;

        if (!weaponsList.Contains(currentWeapon)) return;

        int weaponIndex = Array.IndexOf(weaponsList, currentWeapon);

        if (!string.IsNullOrEmpty(attachmentsList[weaponIndex][0]) && attachmentsList[weaponIndex][0].StartsWith("equipped_"))
        {
            player.SetPlayerData("killstreaksState", "icons", 2, icon_misc_attach);
            player.SetPlayerData("killstreaksState", "hasStreak", 2, false);
        }
        else if (!string.IsNullOrEmpty(attachmentsList[weaponIndex][0]))
        {
            player.SetPlayerData("killstreaksState", "icons", 2, icon_misc_attach);
            player.SetPlayerData("killstreaksState", "hasStreak", 2, true);
        }
        else
        {
            player.SetPlayerData("killstreaksState", "icons", 2, 0);
            player.SetPlayerData("killstreaksState", "hasStreak", 2, false);
        }

        if (!string.IsNullOrEmpty(attachmentsList[weaponIndex][1]) && attachmentsList[weaponIndex][1].StartsWith("equipped_"))
        {
            player.SetPlayerData("killstreaksState", "icons", 3, icon_optic);
            player.SetPlayerData("killstreaksState", "hasStreak", 3, false);
        }
        else if (!string.IsNullOrEmpty(attachmentsList[weaponIndex][1]))
        {
            player.SetPlayerData("killstreaksState", "icons", 3, icon_optic);
            player.SetPlayerData("killstreaksState", "hasStreak", 3, true);
        }
        else
        {
            player.SetPlayerData("killstreaksState", "icons", 3, 0);
            player.SetPlayerData("killstreaksState", "hasStreak", 3, false);
        }
    }

    public static void refreshScoreboard(Entity player)
    {
        player.NotifyOnPlayerCommand("+scoreboard:" + player.EntRef, "+scores");
        player.NotifyOnPlayerCommand("-scoreboard:" + player.EntRef, "-scores");
        OnInterval(50, () =>
        {
            if (player.Classname != "player")
            {
                player.ClearField("isViewingScoreboard");
                return false;
            }
            if (!player.GetField<bool>("isViewingScoreboard")) return true;
            player.ShowScoreBoard();
            return true;
        });
    }
    public static void updatePlayersAliveCount(bool bypassAlive = false)
    {
        int count = playersAlive.Count;
        if (bypassAlive) count = Players.Count;
        SetTeamScore("allies", count);
        SetTeamScore("axis", count);
        //SetTeamScore("none", count);
        playerCount.SetText(count + "/" + totalPlayerCount);
    }
    private static void pickupItem(Entity player, Entity item)
    {
        if (item.HasField("weapon"))
        {
            if (player.IsSwitchingWeapon())
                return;
            //Utilities.PrintToConsole("Picking up " + item.GetField<string>("weapon"));
            string weapon = item.GetField<string>("weapon");
            giveWeapon(player, weapon, item.GetField<int>("ammo"));
            item.ClearField("weapon");
        }
        else if (item.HasField("value"))
        {
            //Utilities.PrintToConsole("Picking up Armor worth " + item.GetField<int>("value"));
            if (player.GetField<int>("shield") >= 200)
            {
                player.IPrintLnBold("Shield full.");
                return;
            }
            int shield = player.GetField<int>("shield");
            shield += item.GetField<int>("value");
            if (shield > 200) shield = 200;
            player.SetField("shield", shield);
            updatePlayerHealthHud(player);
            player.PlayLocalSound("item_blast_shield_on");
            hudPickup(player, "Picked up ^5+" + item.GetField<int>("value") + " Shield!");
            item.ClearField("item");
        }
        else if (item.HasField("attachment"))
        {
            //Utilities.PrintToConsole("Picking up " + item.GetField<string>("attachment"));
            //give attachment in proper slot
            giveAttachment(player, item);
            return;//Returning so giveAttachment can handle deleting
        }
        else if (item.HasField("perk"))
        {
            //Utilities.PrintToConsole("Picking up " + item.GetField<string>("perk"));
            player.PlayLocalSound("earn_perk");
            _givePerk(player, item.GetField<string>("perk"));
            item.ClearField("perk");
        }
        else if (item.HasField("ammoType"))
        {
            //Utilities.PrintToConsole("Picking up " + item.GetField<string>("ammoType") + " Ammo");
            string ammoType = item.GetField<string>("ammoType");
            int amount = item.GetField<int>("amount");
            //if (ammoType == "Shotgun") amount = 4;
            //else if (ammoType == "Sniper") amount = 5;
            player.SetField(item.GetField<string>("ammoType") + "Ammo", player.GetField<int>(ammoType + "Ammo") + amount);
            updateAllWeaponAmmoCounts(player);
            player.PlayLocalSound("scavenger_pack_pickup");
            hudPickup(player, "Picked up +" + amount + " " + ammoType + " ammo!");
            item.ClearField("ammoType");
        }
        else if (item.HasField("isAirdrop"))
        {
            HudElem progressBar = createPrimaryProgressBar(player, "Opening crate...");
            updateBar(progressBar, 120, 3);

            player.DisableWeapons();

            item.SetField("grabCounter", 0);
            OnInterval(50, () => playerOpenCrate(player, item));
            return;
        }
        else if (player.GetField<int>("healthPacks") < 3)//Health
        {
            //Utilities.PrintToConsole("Picking up Health");
            int packs = player.GetField<int>("healthPacks");
            packs++;
            //if (packs > 3) packs = 3;
            player.SetField("healthPacks", packs);
            player.SetPlayerData("killstreaksState", "hasStreak", 1, true);
            //player.SetPlayerData("killstreaksState", "countToNext", 3);
            player.SetPlayerData("killstreaksState", "count", packs);
            player.SetPlayerData("killstreaksState", "selectedIndex", 1);
            player.GiveWeapon("lightstick_mp");
            player.PlaySound("chemlight_pu");
            hudPickup(player, "Picked up a ^2Health Stick!");
        }
        else if (player.GetField<int>("healthPacks") >= 3)
        {
            player.IPrintLnBold("Health Sticks full.");
            return;
        }
        usables.Remove(item);
        if (item.HasField("fx"))
        {
            item.GetField<Entity>("fx").Delete();
            item.ClearField("fx");
        }
        if (item.HasField("weapon")) item.ClearField("weapon");
        if (item.HasField("value")) item.ClearField("value");
        if (item.HasField("attachment")) item.ClearField("attachment");
        if (item.HasField("perk")) item.ClearField("perk");
        item.Delete();
    }
    private static bool playerOpenCrate(Entity player, Entity crate)
    {
        int grabCounter = crate.GetField<int>("grabCounter");

        if (grabCounter < 60 && player.UseButtonPressed() && player.IsAlive && player.Classname == "player" && player.SessionTeam != "spectator" && player.Origin.DistanceTo(crate.Origin) < 75)
        {
            grabCounter++;
            crate.SetField("grabCounter", grabCounter);
            return true;
        }
        else if (grabCounter >= 30 && player.UseButtonPressed() && player.IsAlive && player.Classname == "player" && player.SessionTeam != "spectator" && player.Origin.DistanceTo(crate.Origin) < 75)
        {
            if (player.HasField("progressBar"))
            {
                HudElem bar = player.GetField<HudElem>("progressBar");
                destroyPrimaryProgressBar(bar);
                player.ClearField("progressBar");
            }

            player.EnableWeapons();

            crate.ClearField("grabCounter");
            openCrate(crate);
            return false;
        }
        else
        {
            if (player.HasField("progressBar"))
            {
                HudElem bar = player.GetField<HudElem>("progressBar");
                destroyPrimaryProgressBar(bar);
                player.ClearField("progressBar");
            }

            player.EnableWeapons();
            crate.ClearField("grabCounter");
            return false;
        }
    }
    private static void openCrate(Entity crate)
    {
        PlayFX(fx_crateCollectSmoke, crate.Origin);
        crate.PlaySound("crate_impact");

        Vector3 origin = crate.Origin;

        //Spawn the goods
        spawnLoot(origin + new Vector3(0, 0, 40), "armor_200");//200 armor
        spawnLoot(origin.Around(50) + new Vector3(0, 0, 40), "health");//Health

        string goldWeapon = getRandomLootWeapon(RandomIntRange(8, 28));
        if (goldWeapon == "iw5_g18_mp" || goldWeapon == "iw5_skorpion_mp")
            goldWeapon = getRandomLootWeapon(RandomIntRange(8, 16));//Regrab a new gun excluding secondaries
        goldWeapon += "_camo04";
        //Utilities.PrintToConsole("Spawning " + goldWeapon);
        spawnLoot(origin.Around(50) + new Vector3(0, 0, 50), goldWeapon);//Gold weapon

        usables.Remove(crate);
        crate.Delete();
    }
    public static void _givePerk(Entity player, string perkName)
    {
        List<string> perksList = player.GetField<List<string>>("perksList");
        HudElem perkIcons = player.GetField<HudElem>("hud_perks");

        perksList.Add(perkName);
        player.SetPerk("specialty_" + perkName, true, true);
        player.SetField("perksList", new Parameter(perksList));

        string perkHudText = (string)perkIcons.GetField("text") + createHudShaderString(getPerkIcon(perkName), false, 48, 48);
        perkIcons.SetText(perkHudText);
        perkIcons.SetField("text", perkHudText);
    }
    public static void giveAttachment(Entity player, Entity item)
    {
        string attachment = item.GetField<string>("attachment");
        string[][] attachmentsList = player.GetField<string[][]>("attachmentsList");
        string[] weaponsList = player.GetField<string[]>("weaponsList");

        if (!weaponsList.Contains(player.CurrentWeapon))
            return;

        int index = Array.IndexOf(weaponsList, player.CurrentWeapon);
        if (index > 1)
            return;

        int attachmentSlot = getAttachmentSlot(attachment);

        if (!string.IsNullOrEmpty(attachmentsList[index][attachmentSlot]))
        {
            if (attachmentsList[index][attachmentSlot].StartsWith("equipped_"))
            {
                player.IPrintLnBold("You must unequip your current attachment to pick up a new one.");
                return;
            }
            dropAttachment(player, index, attachmentSlot);
        }

        usables.Remove(item);
        item.ClearField("attachment");
        if (item.HasField("fx"))
        {
            item.GetField<Entity>("fx").Delete();
            item.ClearField("fx");
        }
        item.Delete();

        player.PlayLocalSound("clip_out");//grenade_pickup

        attachmentsList[index][attachmentSlot] = attachment;

        player.SetField("attachmentsList", new Parameter(attachmentsList));
        updateAttachments(player);
    }
    private static void equipAttachment(Entity player, int slot)
    {
        string[][] attachmentsList = player.GetField<string[][]>("attachmentsList");
        string[] weaponsList = player.GetField<string[]>("weaponsList");

        if (!weaponsList.Contains(player.CurrentWeapon))
            return;

        int index = Array.IndexOf(weaponsList, player.CurrentWeapon);
        if (index > 1)
            return;

        string attachment = attachmentsList[index][slot];
        if (string.IsNullOrEmpty(attachment))
            return;

        if (attachment.StartsWith("equipped_"))
            attachment = attachment.Remove(0, 9);

        if (!canAttachAttachment(attachment, weaponsList[index]))
        {
            player.IPrintLnBold("Cannot attach this attachment on this weapon!");
            return;
        }

        List<string> tokenizedWeapon = weaponsList[index].Split('_').ToList();
        //0 is iw5
        //1 is weapon name
        //2 is mp
        //3 is the start of attachments/camo

        string weaponName = tokenizedWeapon[0] + "_" + tokenizedWeapon[1];
        string attachmentForWeapon = getAttachmentForWeapon(attachment, weaponName);

        if (!tokenizedWeapon.Contains(attachmentForWeapon))//Attach this attachment
        {
            tokenizedWeapon.Insert(3, attachmentForWeapon);
            attachmentsList[index][slot] = "equipped_" + attachmentsList[index][slot];
            player.PlayLocalSound("counter_uav_activate");//enable_activeperk
        }
        else if (tokenizedWeapon.Contains(attachmentForWeapon))//remove this attachment
        {
            int currentIndex = tokenizedWeapon.IndexOf(attachmentForWeapon);
            tokenizedWeapon.RemoveAt(currentIndex);
            attachmentsList[index][slot] = attachment;
            player.PlayLocalSound("counter_uav_deactivate");//enable_activeperk
        }

        //Sort the attachments
        if (tokenizedWeapon.Count > 4 && !tokenizedWeapon[4].StartsWith("camo"))
        {
            if (string.Compare(tokenizedWeapon[3], tokenizedWeapon[4]) > 0)
            {
                string first = tokenizedWeapon[4];
                tokenizedWeapon[4] = tokenizedWeapon[3];
                tokenizedWeapon[3] = first;
            }
        }

        string newWeapon = string.Join("_", tokenizedWeapon);
        int clip = player.GetWeaponAmmoClip(weaponsList[index]);
        player.TakeWeapon(weaponsList[index]);
        player.GiveWeapon(newWeapon);
        player.SwitchToWeaponImmediate(newWeapon);
        player.SetWeaponAmmoClip(newWeapon, clip);
        weaponsList[index] = newWeapon;
        player.SetField("weaponsList", new Parameter(weaponsList));
        player.SetField("attachmentsList", new Parameter(attachmentsList));

        updateAllWeaponAmmoCounts(player);
    }
    private static int getAttachmentSlot(string attachment)
    {
        if (Utilities.GetAttachmentType(attachment) == "rail") return 1;
        else return 0;
    }
    public static void giveWeapon(Entity player, string newWeapon, int clip)
    {
        if (!isValidWeapon(newWeapon)) return;
        string[] weaponsList = player.GetField<string[]>("weaponsList");

        string ammoType = getAmmoType(newWeapon);

        if (!newWeapon.StartsWith("iw5_") && newWeapon != "c4death_mp")//Grenades
        {
            player.GiveWeapon(newWeapon);
            player.GiveMaxAmmo(newWeapon);
            player.PlayLocalSound("oldschool_pickup");

            if (newWeapon == "frag_grenade_mp")
            {
                replaceOffhand(player, "primary", "frag_grenade_mp");
                player.SetOffhandPrimaryClass("frag");
            }
            else if (newWeapon == "c4_mp")
            {
                replaceOffhand(player, "primary", "c4_mp");
                player.SetOffhandPrimaryClass("other");
            }
            else if (newWeapon == "throwingknife_mp")
            {
                replaceOffhand(player, "primary", "throwingknife_mp");
                player.SetOffhandPrimaryClass("throwingknife");
            }
            if (newWeapon == "flash_grenade_mp")
            {
                replaceOffhand(player, "secondary", "flash_grenade_mp");
                player.SetOffhandSecondaryClass("flash");
            }
            else if (newWeapon == "concussion_grenade_mp" || newWeapon == "smoke_grenade_mp")
            {
                if (newWeapon == "smoke_grenade_mp") replaceOffhand(player, "secondary", "smoke_grenade_mp");
                else if (newWeapon == "concussion_grenade_mp") replaceOffhand(player, "secondary", "concussion_grenade_mp");
                player.SetOffhandSecondaryClass("smoke");
            }

            player.SetField("weaponsList", new Parameter(weaponsList));
            return;
        }

        if (string.IsNullOrEmpty(weaponsList[0]))
        {
            weaponsList[0] = newWeapon;
            player.GiveWeapon(newWeapon);
            player.SetWeaponAmmoClip(newWeapon, clip);
            if (ammoType != "") player.SetWeaponAmmoStock(newWeapon, player.GetField<int>(ammoType + "Ammo"));
            else player.SetWeaponAmmoStock(newWeapon, 0);
            player.SwitchToWeapon(newWeapon);
        }
        else if (string.IsNullOrEmpty(weaponsList[1]))
        {
            weaponsList[1] = newWeapon;
            player.GiveWeapon(newWeapon);
            player.SetWeaponAmmoClip(newWeapon, clip);
            if (ammoType != "") player.SetWeaponAmmoStock(newWeapon, player.GetField<int>(ammoType + "Ammo"));
            else player.SetWeaponAmmoStock(newWeapon, 0);
            player.SwitchToWeapon(newWeapon);
        }
        else
        {
            string currentWeapon = player.CurrentWeapon;
            dropWeapon(player, player.CurrentWeapon, true);

            if (weaponsList.Contains(currentWeapon))
                weaponsList[Array.IndexOf(weaponsList, currentWeapon)] = newWeapon;//Set weapon in accordance of index
            else weaponsList[0] = newWeapon;

            player.GiveWeapon(newWeapon);
            player.SetWeaponAmmoClip(newWeapon, clip);
            player.SetWeaponAmmoStock(newWeapon, player.GetField<int>(ammoType + "Ammo"));
            player.SwitchToWeaponImmediate(newWeapon);
        }
        player.PlayLocalSound("oldschool_pickup");

        player.SetField("weaponsList", new Parameter(weaponsList));
    }
    private static void replaceOffhand(Entity player, string slot, string item)
    {
        if (player.GetField<string>(slot + "Offhand") != "none" && player.GetAmmoCount(player.GetField<string>(slot + "Offhand")) > 0)
            dropWeapon(player, player.GetField<string>(slot + "Offhand"), true);
        player.SetField(slot + "Offhand", item);
    }
    private static void dropAttachment(Entity player, int weaponIndex, int attachmentIndex)
    {
        string[][] attachmentsList = player.GetField<string[][]>("attachmentsList");

        Entity attachment = Spawn("script_model", player.GetEye());
        attachment.SetModel(getAttachmentModel(attachmentsList[weaponIndex][attachmentIndex]));
        attachment.Angles = Vector3.Zero;
        physicsLaunch(attachment, (Vector3.RandomXY() * 70) + new Vector3(0, 0, 20));
        attachment.SetField("attachment", attachmentsList[weaponIndex][attachmentIndex]);
        AfterDelay(2000, () => usables.Add(attachment));
    }
    private static void dropWeapon(Entity player, string weapon, bool overrideList = false, bool useRecordedAmmo = false)
    {
        string[] weaponList = player.GetField<string[]>("weaponsList");
        //if (!weaponList.Contains(weapon)) return;
        if (weapon == "none" || string.IsNullOrEmpty(weapon)) return;

        if (weapon == "c4death_mp")
        {
            player.TakeWeapon(weapon);
            int index = Array.IndexOf(weaponList, weapon);
            if (weaponList.Contains(weapon)) weaponList[index] = null;
            player.SetField("weaponsList", new Parameter(weaponList));
            return;
        }
        else if (weapon != "c4death_mp" && !weapon.StartsWith("iw5_"))//Grenades
        {
            if (weapon == "frag_grenade_mp" || weapon == "c4_mp" || weapon == "throwingknife_mp")
            {
                player.SetOffhandPrimaryClass("none");
                player.SetField("primaryOffhand", "none");
            }
            else if (weapon == "flash_grenade_mp" || weapon == "concussion_grenade_mp" || weapon == "smoke_grenade_mp")
            {
                player.SetOffhandSecondaryClass("none");
                player.SetField("secondaryOffhand", "none");
            }

            string equipmentModel = GetWeaponModel(weapon);
            if (weapon == "frag_grenade_mp") equipmentModel = "weapon_m67_grenade";
            Entity item = Spawn("script_model", player.GetEye());
            item.SetModel(equipmentModel);
            item.Angles = new Vector3(0, RandomInt(360), 0);
            item.Show();
            physicsLaunch(item, (Vector3.RandomXY() * 70) + new Vector3(0, 0, 20));
            StartAsync(spawnFXOnWeaponDrop(weapon, item));
            item.SetField("weapon", weapon);
            if (useRecordedAmmo)
                item.SetField("ammo", player.GetField<int>(weapon + "_ammo"));
            else
                item.SetField("ammo", player.GetAmmoCount(weapon));

            AfterDelay(2000, () => usables.Add(item));
            return;
        }

        List<string> tokenizedWeapon = weapon.Split('_').ToList();
        if (tokenizedWeapon.Count > 3 && !tokenizedWeapon[3].StartsWith("camo") && !tokenizedWeapon[3].Contains("scope"))
        {
            tokenizedWeapon.RemoveAt(3);
        }
        //Check again for a second attachment
        if (tokenizedWeapon.Count > 3 && !tokenizedWeapon[3].StartsWith("camo") && !tokenizedWeapon[3].Contains("scope"))
        {
            tokenizedWeapon.RemoveAt(3);
        }

        string baseWeapon = string.Join("_", tokenizedWeapon);

        string model = GetWeaponModel(baseWeapon);
        if (baseWeapon == "frag_grenade_mp") model = "weapon_m67_grenade";
        Entity weaponGfx = Spawn("script_model", player.GetEye());
        weaponGfx.SetModel(model);
        weaponGfx.Angles = new Vector3(0, RandomInt(360), 0);
        weaponGfx.Show();
        physicsLaunch(weaponGfx, (Vector3.RandomXY() * 70) + new Vector3(0, 0, 20));
        StartAsync(spawnFXOnWeaponDrop(baseWeapon, weaponGfx));

        weaponGfx.SetField("weapon", baseWeapon);
        if (useRecordedAmmo)
        {
            weaponGfx.SetField("ammo", player.GetField<int>(weapon + "_ammo"));
        }
        else
        {
            if (WeaponInventoryType(baseWeapon) == "offhand")
                weaponGfx.SetField("ammo", player.GetAmmoCount(weapon));
            else
                weaponGfx.SetField("ammo", player.GetWeaponAmmoClip(weapon));
        }

        player.TakeWeapon(weapon);
        string[][] attachmentsList = player.GetField<string[][]>("attachmentsList");
        int weaponIndex = Array.IndexOf(weaponList, weapon);
        if (!string.IsNullOrEmpty(attachmentsList[weaponIndex][0]) && attachmentsList[weaponIndex][0].StartsWith("equipped_"))
            attachmentsList[weaponIndex][0] = attachmentsList[weaponIndex][0].Remove(0, 9);
        if (!string.IsNullOrEmpty(attachmentsList[weaponIndex][1]) && attachmentsList[weaponIndex][1].StartsWith("equipped_"))
            attachmentsList[weaponIndex][1] = attachmentsList[weaponIndex][1].Remove(0, 9);

        if (!overrideList)
        {
            int index = Array.IndexOf(weaponList, weapon);
            weaponList[index] = null;
            player.SetField("weaponsList", new Parameter(weaponList));
        }

        AfterDelay(2000, () => usables.Add(weaponGfx));
        //updateWeaponsHud(player);
    }
    private static IEnumerator spawnFXOnWeaponDrop(string weapon, Entity weaponGfx)
    {
        //Parameter[] param = null;
        yield return Entity.GetEntity(2046).WaitTill("physics_finished");

        //Entity weapRet = (Entity)param[0];

        /*
        if (weapRet != weaponGfx)
        {
            StartAsync(spawnFXOnWeaponDrop(weapon, weaponGfx));
            yield break;
        }
        */

        Entity fx = SpawnFX(getWeaponFXColor(weapon), weaponGfx.Origin);
        TriggerFX(fx);
        weaponGfx.SetField("fx", fx);
        if (getWeaponFXColor(weapon) == fx_glow_gold || getWeaponFXColor(weapon) == fx_glow_grey)
        {
            OnInterval(50, () =>
            {
                    //fx.Origin = weaponGfx.Origin;
                    TriggerFX(fx);
                if (weaponGfx.HasField("fx")) return true;
                else return false;
            });
        }
    }
    private static void recordPlayerAmmoValues(Entity player)
    {
        string[] weaponsList = player.GetField<string[]>("weaponsList").ToArray();
        foreach (string weapon in weaponsList)
        {
            if (weapon != "c4death_mp" && !string.IsNullOrEmpty(weapon))
                player.SetField(weapon + "_ammo", player.GetWeaponAmmoClip(weapon));
        }
        string primaryOffhand = player.GetField<string>("primaryOffhand");
        string secondaryOffhand = player.GetField<string>("secondaryOffhand");
        if (primaryOffhand != "none") player.SetField(primaryOffhand + "_ammo", player.GetWeaponAmmoClip(primaryOffhand));
        if (secondaryOffhand != "none") player.SetField(primaryOffhand + "_ammo", player.GetWeaponAmmoClip(secondaryOffhand));
    }
    private static void dropAllPlayerWeapons(Entity player)
    {
        string[] weaponsList = player.GetField<string[]>("weaponsList");
        foreach (string weapon in weaponsList)
        {
            if (!string.IsNullOrEmpty(weapon) && weapon != "c4death_mp")
                dropWeapon(player, weapon, true, true);
        }
        string primaryOffhand = player.GetField<string>("primaryOffhand");
        string secondaryOffhand = player.GetField<string>("secondaryOffhand");
        if (!string.IsNullOrEmpty(primaryOffhand) && primaryOffhand != "none" && (player.HasField(primaryOffhand + "_ammo") && player.GetField<int>(primaryOffhand + "_ammo") > 0)) dropWeapon(player, primaryOffhand, true, true);
        if (!string.IsNullOrEmpty(secondaryOffhand) && secondaryOffhand != "none" && (player.HasField(secondaryOffhand + "_ammo") && player.GetField<int>(secondaryOffhand + "_ammo") > 0)) dropWeapon(player, secondaryOffhand);
        //clearPlayerWeaponsList(player);
    }
    private static void dropAllPlayerAmmo(Entity player)
    {
        if (player.GetField<int>("Assault RifleAmmo") > 0)
        {
            Entity ammo = Spawn("script_model", player.GetEye());
            ammo.SetModel("weapon_scavenger_grenadebag");
            ammo.Angles = new Vector3(90, 0, 0);
            ammo.PhysicsLaunchServer(ammo.Origin, (Vector3.RandomXY() * 5) + new Vector3(0, 0, 20));
            ammo.SetField("ammoType", "Assault Rifle");
            ammo.SetField("amount", player.GetField<int>("Assault RifleAmmo"));
            AfterDelay(2000, () => usables.Add(ammo));
            player.SetField("Assault RifleAmmo", 0);
        }
        if (player.GetField<int>("SMGAmmo") > 0)
        {
            Entity ammo = Spawn("script_model", player.GetEye());
            ammo.SetModel("weapon_scavenger_grenadebag");
            ammo.Angles = new Vector3(90, 0, 0);
            ammo.PhysicsLaunchServer(ammo.Origin, (Vector3.RandomXY() * 5) + new Vector3(0, 0, 20));
            ammo.SetField("ammoType", "SMG");
            ammo.SetField("amount", player.GetField<int>("SMGAmmo"));
            AfterDelay(2000, () => usables.Add(ammo));
            player.SetField("SMGAmmo", 0);
        }
        if (player.GetField<int>("ShotgunAmmo") > 0)
        {
            Entity ammo = Spawn("script_model", player.GetEye());
            ammo.SetModel("weapon_scavenger_grenadebag");
            ammo.Angles = new Vector3(90, 0, 0);
            ammo.PhysicsLaunchServer(ammo.Origin, (Vector3.RandomXY() * 5) + new Vector3(0, 0, 20));
            ammo.SetField("ammoType", "Shotgun");
            ammo.SetField("amount", player.GetField<int>("ShotgunAmmo"));
            AfterDelay(2000, () => usables.Add(ammo));
            player.SetField("ShotgunAmmo", 0);
        }
        if (player.GetField<int>("SniperAmmo") > 0)
        {
            Entity ammo = Spawn("script_model", player.GetEye());
            ammo.SetModel("weapon_scavenger_grenadebag");
            ammo.Angles = new Vector3(90, 0, 0);
            ammo.PhysicsLaunchServer(ammo.Origin, (Vector3.RandomXY() * 5) + new Vector3(0, 0, 20));
            ammo.SetField("ammoType", "Sniper");
            ammo.SetField("amount", player.GetField<int>("SniperAmmo"));
            AfterDelay(2000, () => usables.Add(ammo));
            player.SetField("SniperAmmo", 0);
        }
    }
    private static void dropAllPlayerHealth(Entity player)
    {
        for (int i = player.GetField<int>("healthPacks"); i > 0; i--)
        {
            Entity health = Spawn("script_model", player.GetEye());
            health.SetModel("viewmodel_light_stick");
            health.Angles = new Vector3(0, 90, 0);
            physicsLaunch(health, (Vector3.RandomXY() * 80) + new Vector3(0, 0, 30));
            AfterDelay(2000, () => usables.Add(health));
        }
    }
    private static void dropAllPlayerPerks(Entity player)
    {
        List<string> perks = player.GetField<List<string>>("perksList");
        for (int i = 0; i < perks.Count; i++)
        {
            string perkName = perks[i];
            Entity perk = Spawn("script_model", player.GetEye());
            perk.SetModel("viewmodel_uav_radio");
            perk.Angles = new Vector3(0, 90, 0);
            physicsLaunch(perk, (Vector3.RandomXY() * 80) + new Vector3(0, 0, 30));
            perk.SetField("perk", perkName);
            AfterDelay(2000, () => usables.Add(perk));
        }
    }
    private static void dropAllPlayerAttachments(Entity player)
    {
        string[][] attachmentsList = player.GetField<string[][]>("attachmentsList");
        if (!string.IsNullOrEmpty(attachmentsList[0][0]) && !attachmentsList[0][0].StartsWith("equipped_")) dropAttachment(player, 0, 0);
        if (!string.IsNullOrEmpty(attachmentsList[0][1]) && !attachmentsList[0][1].StartsWith("equipped_")) dropAttachment(player, 0, 1);
        if (!string.IsNullOrEmpty(attachmentsList[1][0]) && !attachmentsList[1][0].StartsWith("equipped_")) dropAttachment(player, 1, 0);
        if (!string.IsNullOrEmpty(attachmentsList[1][1]) && !attachmentsList[1][1].StartsWith("equipped_")) dropAttachment(player, 1, 1);
    }
    private static Vector3 getWeaponRarityColor(string weapon)
    {
        if (weapon.Contains("_camo01"))
            return new Vector3(0, .7f, .1f);
        if (weapon.Contains("_camo02"))
            return new Vector3(0, .4f, .7f);
        if (weapon.Contains("_camo03"))
            return new Vector3(.4f, .1f, .9f);
        if (weapon.Contains("_camo04"))
            return new Vector3(.9f, .7f, .1f);
        return new Vector3(.6f, .6f, .6f);
    }
    private static int getWeaponFXColor(string weapon)
    {
        if (weapon.Contains("_camo01") || weapon == "flash_grenade_mp" || weapon == "throwingknife_mp")
            return fx_glow_green;
        if (weapon.Contains("_camo02") || weapon == "concussion_grenade_mp")
            return fx_glow_blue;
        if (weapon.Contains("_camo03") || weapon == "c4_mp")
            return fx_glow_purple;
        if (weapon.Contains("_camo04"))
            return fx_glow_gold;
        return fx_glow_grey;
    }
    private static string getAmmoType(string weapon)
    {
        switch (WeaponClass(weapon))
        {
            case "smg":
            case "mg":
            case "pistol":
                return "SMG";
            case "spread":
                return "Shotgun";
            case "sniper":
                return "Sniper";
            case "rifle":
            default:
                return "Assault Rifle";
        }
    }
    private static string getAttachmentModel(string attachment)
    {
        switch (attachment)
        {
            default: return "prop_suitcase_bomb";
            case "reflex": return "weapon_reflex_iw5";
            case "acog": return "weapon_acog";
            case "grip": return "weapon_remington_foregrip";
            case "heartbeat": return "weapon_heartbeat_iw5";
            case "xmags": return "weapon_ak74u_clip";
            case "eotech": return "weapon_eotech";
            case "silencer": return "weapon_silencer_01";
        }
    }
    private static bool canAttachAttachment(string attachment, string weapon)
    {
        string weaponClass = TableLookup("mp/statstable.csv", 4, "iw5_" + weapon.Split('_')[1], 2);
        switch (attachment)
        {
            case "reflex":
                if (weaponClass == "weapon_machine_pistol" || weaponClass == "weapon_smg" || weaponClass == "weapon_assault" || weaponClass == "weapon_lmg" || weaponClass == "weapon_shotgun") return true;
                break;
            case "acog":
                if (weaponClass == "weapon_smg" || weaponClass == "weapon_assault" || weaponClass == "weapon_lmg"/* || weaponClass == "weapon_sniper"*/) return true;
                break;
            case "grip":
                if (weaponClass == "weapon_shotgun" || weaponClass == "weapon_lmg") return true;
                break;
            case "heartbeat":
                if (weaponClass == "weapon_assault" || weaponClass == "weapon_sniper" || weaponClass == "weapon_lmg") return true;
                break;
            case "xmags":
                if (weaponClass == "weapon_pistol" || weaponClass == "weapon_machine_pistol" || weaponClass == "weapon_smg" || weaponClass == "weapon_assault" || weaponClass == "weapon_lmg" || weaponClass == "weapon_shotgun" || weaponClass == "pistol") return true;
                break;
            case "eotech":
                if (weaponClass == "weapon_machine_pistol" || weaponClass == "weapon_smg" || weaponClass == "weapon_assault" || weaponClass == "weapon_lmg" || weaponClass == "weapon_shotgun") return true;
                break;
            case "silencer":
                if (weaponClass == "weapon_pistol" || weaponClass == "weapon_machine_pistol" || weaponClass == "weapon_smg" || weaponClass == "weapon_assault" || weaponClass == "weapon_lmg" || weaponClass == "weapon_shotgun") return true;
                break;
        }
        return false;
    }
    private static string getAttachmentForWeapon(string attachmentName, string weaponName)
    {
        string weaponClass = TableLookup("mp/statstable.csv", 4, weaponName, 2);
        switch (weaponClass)
        {
            case "weapon_smg":
                if (attachmentName == "reflex")
                    return "reflexsmg";
                else if (attachmentName == "eotech")
                    return "eotechsmg";
                else if (attachmentName == "acog")
                    return "acogsmg";
                else if (attachmentName == "thermal")
                    return "thermalsmg";

                return attachmentName;
            case "weapon_lmg":
                if (attachmentName == "reflex")
                    return "reflexlmg";
                else if (attachmentName == "eotech")
                    return "eotechlmg";

                return attachmentName;
            case "weapon_machine_pistol":
                if (attachmentName == "reflex")
                    return "reflexsmg";
                else if (attachmentName == "eotech")
                    return "eotechsmg";
                else if (attachmentName == "silencer")
                    return "silencer02";

                return attachmentName;

            case "weapon_shotgun":
                if (attachmentName == "silencer")
                    return "silencer03";

                return attachmentName;
            default:
                return attachmentName;
        }
    }
    private static string getAttachmentName(string attachment)
    {
        switch (attachment)
        {
            default: return "an";
            case "reflex": return "Red Dot Sight";
            case "acog": return "ACOG Sight";
            case "grip": return "Foregrip";
            case "heartbeat": return "Heartbeat Sensor";
            case "xmags": return "Extended Mags";
            case "eotech": return "Holographic Sight";
            case "silencer": return "Supressor";
        }
    }
    private static string getPerkName(string perk)
    {
        switch (perk)
        {
            case "fastreload": return "Sleight of Hand";
            case "lightweight": return "Lightweight";
            case "lowprofile": return "Low Profile";
            case "quieter": return "Dead Silence";
            case "stalker": return "Stalker";
            case "rof": return "Rapid Fire";
            case "jumpdive": return "Jumpdive";
            case "fastmantle": return "Fast Mantle";
            case "quickdraw": return "Quickdraw";
            case "fastsprintrecovery": return "Ready Up";
            default: return "a Perk";
        }
    }
    private static string getPerkIcon(string perk)
    {
        switch (perk)
        {
            case "fastreload": return "specialty_fastreload_upgrade";
            case "lightweight": return "iw5_cardicon_burning_runner";
            case "lowprofile": return "specialty_longersprint_upgrade";
            case "quieter": return "specialty_quieter";
            case "stalker": return "specialty_stalker";
            case "rof": return "specialty_bulletpenetration";
            case "jumpdive": return "cardicon_dive";
            case "fastmantle": return "hint_mantle";
            case "quickdraw": return "specialty_quickdraw";
            case "fastsprintrecovery": return "specialty_quickdraw_upgrade";
            default: return "specialty_ap";
        }
    }

    public static void clearPlayerWeaponsList(Entity player)
    {
        string[] newList = new string[2];

        player.SetField("weaponsList", new Parameter(newList));
    }
    public static void clearPlayerPerksList(Entity player)
    {
        List<string> newList = new List<string>();

        player.SetField("perksList", new Parameter(newList));
    }
    public static void clearPlayerAttachmentsList(Entity player)
    {
        string[][] newList = new string[2][];
        newList[0][0] = "";
        newList[1][0] = "";
        newList[0][1] = "";
        newList[1][1] = "";

        player.SetField("attachmentsList", new Parameter(newList));
    }
    private static bool slvrImposter(Entity player)
    {
        Utilities.ExecuteCommand("kickclient " + player.EntRef + " Please do not impersonate the developer.");
        return false;
    }
    private static void physicsLaunch(Entity ent, Vector3 force)
    {
        ent.MoveGravity(force, 5);//5 seconds max time before a timeout
        OnInterval(50, () => checkForPhysicsFinished(ent));
    }
    private static bool checkForPhysicsFinished(Entity ent)
    {
        Vector3 ground = GetGroundPosition(ent.Origin, 1);
        if (ent.Origin.Z < ground.Z + 40)
        {
            ent.MoveTo(ground + new Vector3(0, 0, 5), .05f);
            ent.Origin = ground + new Vector3(0, 0, 5);
            ent.Notify("physics_finished");
            Notify("physics_finished", ent);
            ent.PlaySound("physics_weapon_container_default");
            //ent.Origin = ground + new Vector3(0, 0, 5);
            return false;
        }
        return true;//Hopefully won't bug out, may need to add a sanity check to prevent getting stuck and using another ent
    }
    private static bool isValidWeapon(string weapon)
    {
        if (weapon.Contains("_camo"))
        {
            foreach (string token in weapon.Split('_'))
            {
                if (token.StartsWith("camo"))
                {
                    int id;
                    if (int.TryParse(token.Substring(4), out id))
                    {
                        if (id > 4) return false;
                    }
                }
            }
        }
        return true;
    }
    private static string getPlayerModelsForLevel(bool head)
    {
        switch (GetDvar("mapname"))
        {
            case "mp_plaza2":
            case "mp_seatown":
            case "mp_underground":
            case "mp_aground_ss":
            case "mp_italy":
            case "mp_courtyard_ss":
            case "mp_meteora":
                if (!head) return "mp_body_sas_urban_smg";
                return "head_sas_a";
            case "mp_paris":
                if (!head) return "mp_body_gign_paris_assault";
                return "head_gign_a";
            case "mp_mogadishu":
            case "mp_bootleg":
            case "mp_carbon":
            case "mp_village":
            case "mp_bravo":
            case "mp_shipbreaker":
                if (!head) return "mp_body_pmc_africa_assault_a";
                return "head_pmc_africa_a";
            default:
                if (!head) return "mp_body_delta_elite_smg_a";
                return "head_delta_elite_a";
        }
    }

    private static string getRandomPerk()
    {
        int random = RandomInt(10);
        switch (random)
        {
            case 0:
                return "fastreload";
            case 1:
                return "lightweight";
            default:
                return "lowprofile";
            case 3:
                return "quieter";
            case 4:
                return "stalker";
            case 5:
                return "rof";
            case 6:
                return "jumpdive";
            case 7:
                return "fastmantle";
            case 8:
                return "quickdraw";
            case 9:
                return "fastsprintrecovery";
        }
    }
    private static string getRandomAttachment()
    {
        int random = RandomInt(7);
        switch (random)
        {
            default:
                return "reflex";
            case 1:
                return "acog";
            case 2:
                return "grip";
            case 3:
                return "heartbeat";
            case 4:
                return "xmags";
            case 5:
                return "eotech";
            case 6:
                return "silencer";
        }
    }
    private static string getRandomLootWeapon(int selectionOverride = -1)
    {
        int random = RandomInt(50);
        if (selectionOverride != -1) random = selectionOverride;
        switch (random)
        {
            default://Common items, outcome 0-4,35-49
                random = RandomInt(10);
                if (random == 0) return "perk_" + getRandomPerk();
                if (random == 1) return "attachment_" + getRandomAttachment();
                if (random == 2) return "armor_15";
                if (random == 3) return "armor_30";
                if (random == 4) return "armor_60";
                if (random == 5) return "armor_100";
                return "health";
            //case 1:
            //return "armor_15";
            //case 2:
            //return "armor_30";
            //case 3:
            //return "armor_60";
            //case 4:
            //return "armor_100";
            case 5:
                return "iw5_usp45_mp";
            case 6:
                return "iw5_44magnum_mp";
            case 7:
                return "iw5_deserteagle_mp";
            case 8:
                return "iw5_acr_mp";
            case 9:
                return "iw5_type95_mp";
            case 10:
                return "iw5_ak47_mp";
            case 11:
                return "iw5_mk14_mp";
            case 12:
                return "iw5_scar_mp";
            case 13:
                return "iw5_m9_mp";
            case 14:
                return "iw5_p90_mp";
            case 15:
                return "iw5_pp90m1_mp";
            case 16:
                return "iw5_ump45_mp";
            case 17:
                return "iw5_g18_mp";
            case 18:
                return "iw5_skorpion_mp";
            case 19:
                return "iw5_spas12_mp";
            case 20:
                return "iw5_aa12_mp";
            case 21:
                return "iw5_striker_mp";
            case 22:
                return "iw5_1887_mp";
            case 23:
                return "iw5_usas12_mp";
            case 24:
                return "iw5_sa80_mp";
            case 25:
                return "iw5_mg36_mp";
            case 26:
                return "iw5_msr_mp_msrscope";
            case 27:
                return "iw5_l96a1_mp_l96a1scope";
            case 28:
                return "iw5_dragunov_mp_dragunovscope";
            case 29:
                return "c4_mp";
            case 30:
                return "frag_grenade_mp";
            //case 31:
            //return "flash_grenade_mp";
            case 32:
                return "smoke_grenade_mp";
            case 33:
                return "concussion_grenade_mp";
            case 34:
                return "throwingknife_mp";
        }
    }
    /*
    private static string getWeaponAmmoModel(string weapon)
    {
        switch (WeaponClass(weapon))
        {
            case "smg":
                return "weapon_mp5_clip";
            case "spread":
                return "shotgun";
            case "sniper":
                return "weapon_rsass_clip_iw5";
            case "mg":
                return "weapon_m60_clip_iw5";
            case "rifle":
            default:
                return "weapon_ak47_tactical_clip";
        }
    }
    */

    private IEnumerator initLootSpawns()
    {
        switch (GetDvar("mapname"))
        {
            case "mp_dome":
                spawnLoot(new Vector3(139, 174, -385));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(855, -41, -370));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(43, -301, -385));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1340, -413, -375));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(796, 722, -370));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(579, 751, -370));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(451, 415, -380));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-981, 675, -445));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1106, 333, -385));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1514, 1357, -425));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1278, 805, -445));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-386, 667, -275));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(437, 1747, -250));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(482, 2461, -250));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(939, 2030, -250));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1376, 1911, -250));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1203, 1420, -250));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1722, 1342, -250));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(622, 872, -310));
                break;
            case "mp_hardhat":
                spawnLoot(new Vector3(1771, -450, 310));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(2003, 457, 200));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1852, 527, 200));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(2152, 224, 340));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(2024, 895, 335));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1788, 1409, 330));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(728, 1186, 380));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-190, 1109, 420));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(169, 1222, 420));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1279, 835, 300));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1071, 318, 210));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(370, -358, 300));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(0, -355, 310));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(613, -7, 300));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-319, -1558, 300));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(67, -1529, 300));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(197, -1352, 300));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1569, -1222, 325));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1259, 15, 165));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1864, 252, 200));
                break;
            case "mp_radar":
                spawnLoot(new Vector3(-4086, 370, 1357));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-4993, -327, 1177));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-5083, -620, 1170));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-5616, 21, 1196));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-5785, -70, 1196));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-6043, -93, 1196));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-5990, 487, 1308));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-5400, 1283, 1242));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-7151, 2621, 1318));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-7023, 2417, 1318));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-7373, 2442, 1318));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-7321, 3496, 1370));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-7543, 3760, 1370));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-6328, 3794, 1345));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-6011, 3259, 1354));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-4143, 4203, 1218));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-4340, 3105, 1172));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-4711, 2142, 1172));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-4955, 2830, 1179));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-4975, 2597, 1278));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-4422, 1838, 1170));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-3836, 1627, 1174));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-4731, 1250, 1210));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-3507, 1888, 1172));
                break;
            case "mp_lambeth":
                spawnLoot(new Vector3(-803, 228, -208));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-878, 4, -68));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-64, -1008, -237));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(551, -983, -242));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(858, -1282, -230));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1394, 9, -230));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1206, -98, -230));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(2046, 1413, -297));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(2119, 1640, -162));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(2005, 1265, -162));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(876, 1846, -45));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(642, 1542, -45));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(26, -275, -238));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-25, 318, -235));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(579, 1352, -288));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1182, 798, -121));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(2676, 590, -292));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(2820, -568, -260));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1442, -1064, -162));
                break;
            case "mp_plaza2":
                spawnLoot(new Vector3(-215, 2072, 798));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(665, 1362, 666));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(397, 1093, 666));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(401, 751, 666));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(729, 550, 666));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(819, 126, 668));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1093, -284, 666));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1066, -1192, 658));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(828, -1020, 658));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(672, -1167, 658));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-925, -1583, 722));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-332, -878, 658));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-290, -579, 658));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-366, -192, 658));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-939, -203, 626));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1497, -306, 626));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1429, 66, 626));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1672, -108, 630));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-198, -1003, 818));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-118, -587, 818));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-372, -555, 818));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-323, 401, 819));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1064, -393, 818));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1311, 846, 818));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1680, -290, 818));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1106, -1157, 818));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1490, -894, 818));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1165, -646, 818));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-682, 716, 818));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-230, 641, 818));
                break;
            case "mp_mogadishu":
                spawnLoot(new Vector3(501, -943, 79));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(576, -253, -25));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-65, -107, -35));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(482, 560, -20));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-500, -27, -21));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-362, 570, -21));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-483, -15, 114));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-366, 559, 114));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-875, 1254, -13));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-926, 684, 114));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-886, 1214, 114));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1070, 1400, 114));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-883, 1809, 114));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1096, 200, -29));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1095, 6, -29));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1255, 821, -44));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1089, 641, -44));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1401, 563, 111));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1200, 733, 111));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1732, 2239, 66));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1796, 2511, 66));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1411, 2871, 97));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(760, 3563, 245));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(277, 2893, 106));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(323, 2523, 106));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(331, 2597, 242));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(267, 2375, 242));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(314, 1212, -32));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(718, 1221, -31));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-41, -735, -22));
                break;
            case "mp_exchange":
                spawnLoot(new Vector3(835, -2125, 48));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-199, -1760, 54));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-494, -1287, 54));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1209, -431, -3));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-841, -11, 87));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-162, 161, 87));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-645, 115, 87));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-476, -274, 87));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-359, 433, 87));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-646, 137, 247));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-277, 185, 247));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(39, 638, 87));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(115, 159, 88));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-17, -373, 87));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1310, -1629, 74));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1422, -874, 103));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1506, -457, 109));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1720, 410, 92));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(2013, 355, 91));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(2500, 601, 92));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(595, 1398, 98));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1535, 1409, -157));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(613, 1378, -157));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1146, 1077, -157));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(408, -693, -156));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(692, -1098, -157));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(46, -1090, -157));
                break;
            case "mp_paris":
                spawnLoot(new Vector3(-1787, 718, 266));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-2166, -192, 203));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1557, 352, 86));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1866, -903, 147));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1605, -691, 70));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1382, -1132, 70));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1355, -711, 70));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-368, -938, 62));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-70, 348, 122));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-79, 590, 129));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-33, 904, 121));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(120, 806, -2));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-343, 1924, -33));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1702, 1316, 3));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1434, 1317, 3));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1789, 99, 130));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1047, 1159, 130));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(693, -927, 130));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1596, -533, 4));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1117, -252, 5));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(1048, 503, 0));
                yield return WaitTill_notify_or_timeout("physics_finished", 2);
                spawnLoot(new Vector3(-1245, 1302, 138));
                break;
        }
    }
    private static void spawnLoot(Vector3 position, string loot = "")
    {
        string weapon = loot;

        if (weapon == "")
            weapon = getRandomLootWeapon();

        if (string.IsNullOrEmpty(weapon)) return;

        //Utilities.PrintToConsole("Loot spawn is " + weapon);
        if (weapon.StartsWith("armor_"))
        {
            Entity armor = Spawn("script_model", position);
            armor.SetModel("ims_scorpion_explosive1");
            armor.Angles = new Vector3(0, 0, 0);
            physicsLaunch(armor, Vector3.Zero);
            Vector3 ground = GetGroundPosition(armor.Origin, 5);
            armor.SetField("value", int.Parse(weapon.Split('_')[1]));
            usables.Add(armor);
            return;
        }
        if (weapon == "health")
        {
            Entity health = Spawn("script_model", position);
            health.SetModel("viewmodel_light_stick");
            health.Angles = new Vector3(0, 90, 0);
            physicsLaunch(health, Vector3.Zero);
            usables.Add(health);
            return;
        }
        if (weapon.StartsWith("attachment_"))
        {
            Entity attachment = Spawn("script_model", position);
            attachment.SetModel(getAttachmentModel(weapon.Split('_')[1]));
            attachment.Angles = new Vector3(0, 90, 0);
            physicsLaunch(attachment, Vector3.Zero);
            attachment.SetField("attachment", weapon.Split('_')[1]);

            Entity glow = SpawnFX(getWeaponFXColor(weapon), attachment.Origin);
            TriggerFX(glow);
            attachment.SetField("fx", glow);
            OnInterval(50, () =>
            {
                glow.Origin = attachment.Origin;
                TriggerFX(glow);
                if (attachment.HasField("fx")) return true;
                else return false;
            });

            usables.Add(attachment);
            return;
        }
        if (weapon.StartsWith("perk_"))
        {
            Entity perk = Spawn("script_model", position);
            perk.SetModel("viewmodel_uav_radio");
            perk.Angles = new Vector3(0, 90, 0);
            physicsLaunch(perk, Vector3.Zero);
            perk.SetField("perk", weapon.Split('_')[1]);
            usables.Add(perk);
            return;
        }

        if (weapon.StartsWith("iw5_") && (weapon != "iw5_usp45_mp" && weapon != "iw5_44magnum_mp" && weapon != "iw5_deserteagle_mp" && weapon != "iw5_g18_mp" && weapon != "iw5_skorpion_mp") && !weapon.Contains("_camo"))
        {
            int rarity = RandomInt(100);
            if (rarity > 95) weapon += "_camo04";
            else if (rarity > 85) weapon += "_camo03";
            else if (rarity > 70) weapon += "_camo02";
            else if (rarity > 45) weapon += "_camo01";
        }
        //Else is common (0-45)

        string model = GetWeaponModel(weapon);
        if (weapon == "frag_grenade_mp") model = "weapon_m67_grenade";
        if (weapon.StartsWith("iw5_"))
        {
            Entity ammo = Spawn("script_model", position);
            ammo.SetModel("weapon_scavenger_grenadebag");
            ammo.Angles = new Vector3(90, 0, 0);
            ammo.PhysicsLaunchServer(ammo.Origin, Vector3.RandomXY() * 7);
            string ammoType = getAmmoType(weapon);
            ammo.SetField("ammoType", ammoType);
            if (ammoType == "Assault Rifle" || ammoType == "SMG") ammo.SetField("amount", 15);
            else if (ammoType == "Sniper") ammo.SetField("amount", 5);
            else ammo.SetField("amount", 4);
            usables.Add(ammo);
        }
        Entity dropOrigin = Spawn("script_origin", position);
        Entity weaponModel = dropOrigin.DropScavengerBag(weapon);
        dropOrigin.Delete();
        weaponModel.Angles = new Vector3(0, RandomInt(360), 0);
        weaponModel.SetModel(model);
        weaponModel.Show();
        Entity fx = SpawnFX(getWeaponFXColor(weapon), weaponModel.Origin);
        TriggerFX(fx);
        if (getWeaponFXColor(weapon) == fx_glow_gold || getWeaponFXColor(weapon) == fx_glow_grey)
        {
            OnInterval(50, () =>
            {
                fx.Origin = weaponModel.Origin;
                TriggerFX(fx);
                if (weaponModel.HasField("fx")) return true;
                else return false;
            });
        }
        else
        {
            OnInterval(50, () =>
            {
                fx.Origin = weaponModel.Origin;
                    //TriggerFX(fx);
                    if (weaponModel.HasField("fx")) return true;
                else return false;
            });
        }

        weaponModel.SetField("fx", fx);
        weaponModel.SetField("weapon", weapon);
        weaponModel.SetField("ammo", WeaponClipSize(weapon));
        usables.Add(weaponModel);
    }

    #region memory scanning
    public class memoryScanning
    {
        //[DllImport("kernel32.dll")]
        //private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr buffer, uint size, int lpNumberOfBytesRead);
        //[DllImport("kernel32.dll")]
        //private static extern bool WriteProcessMemory(IntPtr hProcess, int lpBaseAddress, [In, Out] byte[] buffer, uint size, out int lpNumberOfBytesWritten);
        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] buffer, uint size, int lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        private static extern int VirtualQuery(IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);
        //[DllImport("kernel32.dll")]
        //private static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }
        private static Dictionary<string, List<IntPtr>> weaponStructs = new Dictionary<string, List<IntPtr>>();
        private static string[] weaponPatches = new string[] { "c4death_mp", "iw5_acr_mp", "iw5_type95_mp", "iw5_ak47_mp", "iw5_mk14_mp", "iw5_g18_mp", "iw5_spas12_mp", "iw5_striker_mp", "iw5_usas12_mp", "iw5_usp45_mp", "iw5_44magnum_mp", "iw5_deserteagle_mp", "iw5_scar_mp", "iw5_m9_mp", "iw5_p90_mp", "iw5_pp90m1_mp", "iw5_ump45_mp", "iw5_skorpion_mp", "iw5_aa12_mp", "iw5_1887_mp", "iw5_sa80_mp", "iw5_mg36_mp", "iw5_msr_mp", "iw5_l96a1_mp", "iw5_dragunov_mp" };

        public static class Mem
        {
            public static string ReadString(int address, int maxlen = 0)
            {
                string ret = "";
                maxlen = (maxlen == 0) ? int.MaxValue : maxlen;

                byte[] buffer = new byte[maxlen];

                ReadProcessMemory(Process.GetCurrentProcess().Handle, new IntPtr(address), buffer, (uint)maxlen, 0);

                ret = Encoding.ASCII.GetString(buffer);

                return ret;
            }

            public static void WriteString(IntPtr address, string str, bool endZero = true)
            {
                if (!canReadAndWriteMemory(address, 1024)) return;

                byte[] strarr = Encoding.ASCII.GetBytes(str);

                Marshal.Copy(strarr, 0, address, strarr.Length);
                if (endZero) Marshal.WriteByte(address + str.Length, 0);
            }
            public static bool canReadMemory(IntPtr address, uint length)
            {
                MEMORY_BASIC_INFORMATION mem;
                VirtualQuery(address, out mem, length);

                if (mem.Protect == 0x40 || mem.Protect == 0x04 || mem.Protect == 0x02) return true;
                return false;
            }
            public static bool canReadAndWriteMemory(IntPtr address, uint length)
            {
                MEMORY_BASIC_INFORMATION mem;
                VirtualQuery(address, out mem, length);

                if (/*mem.Protect == 0x40 || */mem.Protect == 0x04) return true;
                return false;
            }
            public static IntPtr getProcessBaseAddress()
                => Process.GetCurrentProcess().MainModule.BaseAddress;
        }

        public static void scanForWeaponStructs()
        {
            //Utilities.PrintToConsole("Searching for weapon structs...");

            Process p = Process.GetCurrentProcess();
            IntPtr currentAddr = new IntPtr(0x10000000);//Start the scan at 10 for now
            byte[] buffer = new byte[2048];
            string s = null;

            for (; (int)currentAddr < 0x23000000; currentAddr += 2048)
            {
                if (!Mem.canReadMemory(currentAddr, 2048)) continue;

                s = null;
                ReadProcessMemory(p.Handle, currentAddr, buffer, 2048, 0);
                s = Encoding.ASCII.GetString(buffer);

                if (!string.IsNullOrEmpty(s))
                {
                    //Utilities.PrintToConsole("Address " + currentAddr.ToString("X"));
                    for (int i = 0; i < weaponPatches.Length; i++)
                    {
                        if (s.Contains(weaponPatches[i]))
                        {
                            int offset = s.IndexOf(weaponPatches[i]);
                            //if (Marshal.ReadInt16(currentAddr + offset + weaponPatches[i].Length) != 0x00) continue;//If the next two bytes aren't null, this isn't the right block

                            weaponStructs[weaponPatches[i]].Add(currentAddr + offset);
                            //Utilities.PrintToConsole("Address " + (currentAddr + offset).ToString("X") + " found for weapon struct " + weaponPatches[i]);
                        }
                    }
                }
            }
        }

        public static void scanWeaponStructs(object sender, DoWorkEventArgs e)
        {
            scanForWeaponStructs();
        }

        public static void searchWeaponPatchPtrs()
        {
            if (GetDvarInt("scr_damagePatches") == 1) return;

            //init structs dictionary
            foreach (string weapon in weaponPatches)
                weaponStructs.Add(weapon, new List<IntPtr>());

            BackgroundWorker task = new BackgroundWorker();
            task.DoWork += scanWeaponStructs;
            task.RunWorkerAsync();

            task.RunWorkerCompleted += new RunWorkerCompletedEventHandler(scanWeaponStructs_Completed);
            task.RunWorkerCompleted += new RunWorkerCompletedEventHandler((s, e) => task.Dispose());
        }
        private static void scanWeaponStructs_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            //Utilities.PrintToConsole("Searching for weapon structs complete.");
            if (e.Cancelled)
            {
                Utilities.PrintToConsole("Weapon patch search was cancelled for an unknown reason! This may cause bugs to occur for certain weapons.");
                return;
            }
            if (e.Error != null)
            {
                Utilities.PrintToConsole("There was an error finding weapon patch locations: " + e.Error.Message);
                return;
            }

            setupWeaponPatches();
        }

        private static void setupWeaponPatches()
        {
            List<IntPtr> currentPtrs;
            for (int i = 0; i < weaponPatches.Length; i++)
            {
                if (!weaponStructs.ContainsKey(weaponPatches[i]))
                {
                    Utilities.PrintToConsole(string.Format("Could not find weapon data for {0}!", weaponPatches[i]));
                    continue;
                }

                currentPtrs = weaponStructs[weaponPatches[i]];
                foreach (IntPtr ptr in currentPtrs)
                {
                    //Utilities.PrintToConsole("Setting weapon patches for " + weaponPatches[i]);

                    switch (weaponPatches[i])
                    {
                        case "c4death_mp":
                            if (Marshal.ReadInt32(ptr + 0x24C) == 135) handsDamageLoc = ptr + 0x24C;
                            break;
                        case "iw5_acr_mp":
                            if (Marshal.ReadInt32(ptr + 0x244) == 45) acrDamageLoc = ptr + 0x244;
                            break;
                        case "iw5_type95_mp":
                            if (Marshal.ReadInt32(ptr + 0x248) == 55) type95DamageLoc = ptr + 0x248;
                            break;
                        case "iw5_ak47_mp":
                            if (Marshal.ReadInt32(ptr + 0x244) == 49) ak47DamageLoc = ptr + 0x244;
                            break;
                        case "iw5_mk14_mp":
                            if (Marshal.ReadInt32(ptr + 0x244) == 75) mk14DamageLoc = ptr + 0x244;
                            break;
                        case "iw5_g18_mp":
                            if (Marshal.ReadInt32(ptr + 0x244) == 42) g18DamageLoc = ptr + 0x244;
                            break;
                        case "iw5_spas12_mp":
                            if (Marshal.ReadInt32(ptr + 0x248) == 30) spasDamageLoc = ptr + 0x248;
                            break;
                        case "iw5_striker_mp":
                            if (Marshal.ReadInt32(ptr + 0x248) == 25) strikerDamageLoc = ptr + 0x248;
                            break;
                        case "iw5_usas12_mp":
                            if (Marshal.ReadInt32(ptr + 0x248) == 25) usasDamageLoc = ptr + 0x248;
                            break;
                        case "iw5_usp45_mp":
                            if (Marshal.ReadInt32(ptr + 0x248) == 40) uspDamageLoc = ptr + 0x248;
                            break;
                        case "iw5_44magnum_mp":
                            if (Marshal.ReadInt32(ptr + 0x24B) == 49) magnumDamageLoc = ptr + 0x24B;
                            break;
                        case "iw5_deserteagle_mp":
                            if (Marshal.ReadInt32(ptr + 0x24E) == 49) deagleDamageLoc = ptr + 0x24E;
                            break;
                        case "iw5_scar_mp":
                            if (Marshal.ReadInt32(ptr + 0x246) == 35) scarDamageLoc = ptr + 0x246;
                            break;
                        case "iw5_m9_mp":
                            if (Marshal.ReadInt32(ptr + 0x244) == 35) pm9DamageLoc = ptr + 0x244;
                            break;
                        case "iw5_p90_mp":
                            if (Marshal.ReadInt32(ptr + 0x245) == 42) p90DamageLoc = ptr + 0x245;
                            break;
                        case "iw5_pp90m1_mp":
                            if (Marshal.ReadInt32(ptr + 0x247) == 42) pp90DamageLoc = ptr + 0x247;
                            break;
                        case "iw5_ump45_mp":
                            if (Marshal.ReadInt32(ptr + 0x248) == 49) umpDamageLoc = ptr + 0x248;
                            break;
                        case "iw5_skorpion_mp":
                            if (Marshal.ReadInt32(ptr + 0x248) == 30) skorpionDamageLoc = ptr + 0x248;
                            break;
                        case "iw5_aa12_mp":
                            if (Marshal.ReadInt32(ptr + 0x244) == 15) aa12DamageLoc = ptr + 0x244;
                            break;
                        case "iw5_1887_mp":
                            if (Marshal.ReadInt32(ptr + 0x247) == 30) modelDamageLoc = ptr + 0x247;
                            break;
                        case "iw5_sa80_mp":
                            if (Marshal.ReadInt32(ptr + 0x244) == 38) l86DamageLoc = ptr + 0x244;
                            break;
                        case "iw5_mg36_mp":
                            if (Marshal.ReadInt32(ptr + 0x244) == 40) mg36DamageLoc = ptr + 0x244;
                            break;
                        case "iw5_msr_mp":
                            if (Marshal.ReadInt32(ptr + 0x244) == 98) msrDamageLoc = ptr + 0x244;
                            break;
                        case "iw5_l96a1_mp":
                            if (Marshal.ReadInt32(ptr + 0x248) == 98) l96DamageLoc = ptr + 0x248;
                            break;
                        case "iw5_dragunov_mp":
                            if (Marshal.ReadInt32(ptr + 0x24B) == 70) dragunovDamageLoc = ptr + 0x24B;
                            break;
                        default:
                            break;
                    }

                    //Utilities.PrintToConsole("Setting " + weaponPatches[i] + "to " + ptr.ToString("X"));
                }
            }

            AfterDelay(0, patchDamages);
        }
    }
    #endregion
}
