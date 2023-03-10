using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
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
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            PlayerEx playerEx;
            if (!PlayerData.TryGetValue(self.player, out playerEx)) return;

            playerEx.screamSound.Update();
         
            if (!playerEx.isScreaming) return;

            float time = playerEx.screamCounter / 40.0f;

            NoteProperties? currentNote = null;

            for (int i = NOTES.Count - 1; i > 0; i--)
            {
                if (i <= -1) playerEx.screamCounter = 0;

                NoteProperties noteProperty = NOTES[i];
                currentNote = noteProperty;

                if (noteProperty.timeStamp <= time) break;
            }

            if (currentNote == null) return;

            string? face = GetFace(self, currentNote.note);
            if (face != null) SetFaceSprite(sLeaser, face);
        }

        private static void SetFaceSprite(RoomCamera.SpriteLeaser sLeaser, string spriteName)
        {
            if (!Futile.atlasManager.DoesContainElementWithName(spriteName))
            {
                Plugin.Logger.LogError($"Missing sprite ({spriteName})! Please check the sprites directory under the mod's folder");
                return;
            }
            sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName(spriteName);
        }

        private static bool isInit = false;

        private static void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            if (isInit) return;
            isInit = true;

            Enums.RegisterValues();
            MachineConnector.SetRegisteredOI(Plugin.MOD_ID, Options.instance);

            ResourceLoader.LoadSprites();
        }

        private static void RainWorld_OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld self, ModManager.Mod[] newlyDisabledMods) => Enums.UnregisterValues();

        private const int FRAMERATE = 40;
        private const float SCREAM_FRAME_TIMEOUT = 0;


        private static ConditionalWeakTable<Player, PlayerEx> PlayerData = new ConditionalWeakTable<Player, PlayerEx>();

        private class PlayerEx
        {
            public readonly ChunkDynamicSoundLoop screamSound;

            public PlayerEx(Player player)
            {
                screamSound = new ChunkDynamicSoundLoop(player.firstChunk);
                screamSound.sound = Enums.Sounds.SlugScream;
            }

            public bool wantsToScream = false;
            public bool isScreaming = false;

            public int screamCounter = 0;
            public int savedScreamCounter = 0;
            public int screamTimeoutCounter = 0;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            PlayerEx playerEx;
            if (!PlayerData.TryGetValue(self, out playerEx))
            {
                playerEx = new PlayerEx(self);
                PlayerData.Add(self, playerEx);
            }

            HandlePlayerInput(self, Options.keybindPlayer1.Value, 0);
            HandlePlayerInput(self, Options.keybindPlayer2.Value, 1);
            HandlePlayerInput(self, Options.keybindPlayer3.Value, 2);
            HandlePlayerInput(self, Options.keybindPlayer4.Value, 3);

            if (playerEx.wantsToScream)
            {
                if (!playerEx.isScreaming)
                {
                    playerEx.screamCounter = playerEx.savedScreamCounter;
                }

                playerEx.isScreaming = true;
                playerEx.screamSound.Volume = 1.0f;

                playerEx.screamTimeoutCounter = 0;
                playerEx.screamCounter++;
                playerEx.savedScreamCounter = playerEx.screamCounter;
            }
            else
            {
                playerEx.screamCounter = 0;
                playerEx.isScreaming = false;
                playerEx.screamSound.Volume = 0.001f;
               
                if (playerEx.screamTimeoutCounter > SCREAM_FRAME_TIMEOUT)
                {
                    playerEx.savedScreamCounter = 0;
                }
                else
                {
                    playerEx.screamTimeoutCounter++;
                }
            }
        }

        private static void HandlePlayerInput(Player player, KeyCode keyCode, int targetPlayerIndex)
        {
            if (player.playerState.playerNumber != targetPlayerIndex) return;

            PlayerEx playerEx;
            if (!PlayerData.TryGetValue(player, out playerEx)) return;
            playerEx.wantsToScream = Input.GetKey(keyCode) || (targetPlayerIndex == 0 && Input.GetKey(Options.keybindKeyboard.Value));
        }

        private static void StartScreaming()
        {

        }

        private static void StopScreaming()
        {

        }

        private static readonly List<NoteProperties> NOTES = new List<NoteProperties>()
        {
            new NoteProperties(0.0f, Note.MEDIUM),
            new NoteProperties(7.0f, Note.HIGH),
            new NoteProperties(10.0f, Note.MEDIUM),
            new NoteProperties(20.0f, Note.LOW),
            new NoteProperties(30.0f, Note.VERY_LOW),
            new NoteProperties(40.0f, Note.MEDIUM),
            new NoteProperties(50.0f, Note.HIGH),
            new NoteProperties(60.0f, Note.MEDIUM),
            new NoteProperties(70.0f, Note.VERY_HIGH),
            new NoteProperties(80.0f, Note.HIGH),
            new NoteProperties(90.0f, Note.VERY_HIGH),
            new NoteProperties(100.0f, Note.HIGH),
            new NoteProperties(110.0f, Note.MEDIUM),
        };

        class NoteProperties
        {
            public NoteProperties(float timeStamp, Note note)
            {
                this.timeStamp = timeStamp;
                this.note = note;
            }

            public float timeStamp;
            public Note note;
        }

        private enum Note
        {
            NONE,
            VERY_LOW,
            LOW,
            MEDIUM,
            HIGH,
            VERY_HIGH
        }

        private static string? GetFace(PlayerGraphics self, Note note)
        {
            if (note == Note.NONE) return null;

            SlugcatStats.Name name = self.player.SlugCatClass;
            string face = "default";

            if (self.player.dead)
            {
                face = "dead";
            }
            else if (name == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer)
            {
                face = "artificer";
            }
            else if (name == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                face = "saint";
            }

            string suffix = note switch
            {
                Note.VERY_LOW => "verylow",
                Note.LOW => "low",
                Note.MEDIUM => "medium",
                Note.HIGH => "high",
                Note.VERY_HIGH => "veryhigh",

                _ => ""
            };

            face += "_" + suffix;

            return Plugin.MOD_ID + "_" + face;
        }
    }
}
