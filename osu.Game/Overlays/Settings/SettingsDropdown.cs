﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Graphics.UserInterface;
using osuTK;

namespace osu.Game.Overlays.Settings
{
    public class SettingsDropdown<T> : SettingsItem<T>
    {
        protected new OsuDropdown<T> Control => (OsuDropdown<T>)base.Control;

        public IEnumerable<T> Items
        {
            get => Control.Items;
            set => Control.Items = value;
        }

        public IBindableList<T> ItemSource
        {
            get => Control.ItemSource;
            set => Control.ItemSource = value;
        }

        public override IEnumerable<string> FilterTerms => base.FilterTerms.Concat(Control.Items.Select(i => i.ToString()));

        public SettingsDropdown()
        {
            FlowContent.Spacing = new Vector2(0, 10);
        }

        protected sealed override Drawable CreateControl() => CreateDropdown();

        protected virtual OsuDropdown<T> CreateDropdown() => new DropdownControl();

        protected class DropdownControl : OsuDropdown<T>
        {
            public DropdownControl()
            {
                RelativeSizeAxes = Axes.X;
            }

            protected override DropdownMenu CreateMenu() => base.CreateMenu().With(m => m.MaxHeight = 200);
        }
    }
}
