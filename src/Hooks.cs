using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SlugScream
{
    internal static class Hooks
    {
        public static void ApplyHooks()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.RainWorld.OnModsDisabled += RainWorld_OnModsDisabled;

            On.Player.Update += Player_Update;
        }

        private static bool isInit = false;

        private static void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            if (isInit) return;
            isInit = true;

            Enums.RegisterValues();
            MachineConnector.SetRegisteredOI(Plugin.MOD_ID, Options.instance);

            try
            {

            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
            }
        }

        private static void RainWorld_OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld self, ModManager.Mod[] newlyDisabledMods) => Enums.UnregisterValues();

        private const float DELTA_TIME = 1.0f / 40;
        private const int MAX_NUMBER_OF_PLAYERS = 4;

        private const float TIMEOUT_TIME = 3.0f;

        private readonly static bool[] isPlayerKeyPressed = new bool[MAX_NUMBER_OF_PLAYERS];
        private readonly static bool[] isPlayerScreaming = new bool[MAX_NUMBER_OF_PLAYERS];
        private readonly static int[] playerScreamTimers = new int[MAX_NUMBER_OF_PLAYERS];
        private readonly static int[] playerScreamTimeoutTimers = new int[MAX_NUMBER_OF_PLAYERS];

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            HandlePlayerInput(self, Options.screamKeybindPlayer1.Value, 0);
            HandlePlayerInput(self, Options.screamKeybindPlayer2.Value, 1);
            HandlePlayerInput(self, Options.screamKeybindPlayer3.Value, 2);
            HandlePlayerInput(self, Options.screamKeybindPlayer4.Value, 3);
        }

        private static void HandlePlayerInput(Player player, KeyCode keyCode, int targetPlayerIndex)
        {
            // Handle only corresponding player
            int playerIndex = player.playerState.playerNumber;
            if (playerIndex != targetPlayerIndex) return;

            if (Input.GetKey(keyCode) || (playerIndex == 0 && Input.GetKey(Options.screamKeybindKeyboard.Value)))
            {
                // On press
                if (!isPlayerKeyPressed[playerIndex])
                {

                }

                playerScreamTimeoutTimers[playerIndex] = 0;
                playerScreamTimers[playerIndex]++;
                isPlayerScreaming[playerIndex] = true;

                isPlayerKeyPressed[playerIndex] = true;
            }
            else
            {
                if (playerScreamTimeoutTimers[playerIndex] > TIMEOUT_TIME / DELTA_TIME)
                {
                    isPlayerScreaming[playerIndex] = false;
                }
             
                isPlayerKeyPressed[playerIndex] = false;
            }
        }
    }
}
