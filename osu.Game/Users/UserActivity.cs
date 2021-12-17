﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets;
using osuTK.Graphics;

namespace osu.Game.Users
{
    public abstract class UserActivity
    {
        public abstract string Status { get; }
        public virtual Color4 GetAppropriateColour(OsuColour colours) => colours.GreenDarker;

        public virtual string GetDetails(DiscordRichPresenceMode privacyMode) => string.Empty;

        public class Modding : UserActivity
        {
            public override string Status => "Modding a map";
            public override Color4 GetAppropriateColour(OsuColour colours) => colours.PurpleDark;
        }

        public class ChoosingBeatmap : UserActivity
        {
            public override string Status => "Choosing a beatmap";
        }

        public abstract class InGame : UserActivity
        {
            public IBeatmapInfo BeatmapInfo { get; }

            public IRulesetInfo Ruleset { get; }

            protected InGame(IBeatmapInfo beatmapInfo, IRulesetInfo ruleset)
            {
                BeatmapInfo = beatmapInfo;
                Ruleset = ruleset;
            }

            public override string Status => Ruleset.CreateInstance().PlayingVerb;

            public override string GetDetails(DiscordRichPresenceMode privacyMode)
            {
                return BeatmapInfo.ToString();
            }
        }

        public class InMultiplayerGame : InGame
        {
            public InMultiplayerGame(IBeatmapInfo beatmapInfo, IRulesetInfo ruleset)
                : base(beatmapInfo, ruleset)
            {
            }

            public override string Status => $@"{base.Status} with others";
        }

        public class InPlaylistGame : InGame
        {
            public InPlaylistGame(IBeatmapInfo beatmapInfo, IRulesetInfo ruleset)
                : base(beatmapInfo, ruleset)
            {
            }
        }

        public class InSoloGame : InGame
        {
            private readonly int restartCount;

            public InSoloGame(IBeatmapInfo beatmapInfo, IRulesetInfo ruleset, int restartCount)
                : base(beatmapInfo, ruleset)
            {
                this.restartCount = restartCount;
            }

            public override string GetDetails(DiscordRichPresenceMode privacyMode)
            {
                return (restartCount == 0 ? string.Empty : $"({restartCount + 1}x) ") + BeatmapInfo;
            }
        }

        public class Editing : UserActivity
        {
            public IBeatmapInfo BeatmapInfo { get; }

            public Editing(IBeatmapInfo info)
            {
                BeatmapInfo = info;
            }

            public override string Status => @"Editing a beatmap";

            public override string GetDetails(DiscordRichPresenceMode privacyMode)
            {
                return BeatmapInfo.ToString();
            }
        }

        public class Spectating : UserActivity
        {
            public override string Status => @"Spectating a game";
        }

        public class SearchingForLobby : UserActivity
        {
            public override string Status => @"Looking for a lobby";
        }

        public class InLobby : UserActivity
        {
            public override string Status => @"In a lobby";

            public readonly Room Room;

            public InLobby(Room room)
            {
                Room = room;
            }

            public override string GetDetails(DiscordRichPresenceMode privacyMode)
            {
                return privacyMode == DiscordRichPresenceMode.Limited ? string.Empty : Room.Name.Value;
            }
        }
    }
}
