// Copyright 2021 Jamie Taylor
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;

namespace ToDew {
    public class OverlayConfig {
        public bool enabled = true;
        public SButton hotkey = SButton.None;
        public KeybindList hotkeyList = new KeybindList();
        public bool hideAtFestivals = false;
        public int maxWidth = 600;
        public int maxItems = 10;
        public Color backgroundColor = Color.Black * 0.2f;
        public Color textColor = Color.White * 0.8f;
        public static void RegisterConfigMenuOptions(Func<OverlayConfig> getThis, GenericModConfigMenuAPI api, IManifest modManifest) {
            api.RegisterLabel(modManifest, "Overlay", "Configure the always-on overlay showing the list");
            api.RegisterSimpleOption(modManifest, "Enabled", "Is the overlay enabled?", () => getThis().enabled, (bool val) => getThis().enabled = val);
            api.RegisterSimpleOption(modManifest, "Hotkey", "Hotkey to show or hide", () => getThis().hotkey, (SButton val) => getThis().hotkey = val);
            api.RegisterSimpleOption(modManifest, "Hide at festivals", "Hide the overlay during festivals?", () => getThis().hideAtFestivals, (bool val) => getThis().hideAtFestivals = val);
            api.RegisterSimpleOption(modManifest, "Max Width", "Maximum width of the overlay in pixels", () => getThis().maxWidth, (int val) => getThis().maxWidth = val);
            api.RegisterSimpleOption(modManifest, "Max Items", "Maximum number of items to show in the overlay", () => getThis().maxItems, (int val) => getThis().maxItems = val);
        }
    }
    public class ToDoOverlay : IDisposable {
        private readonly ModEntry theMod;
        private readonly ToDoList theList;
        private readonly OverlayConfig config;
        private const string ListHeader = "To-Dew List";
        private const int marginTop = 5;
        private const int marginLeft = 5;
        private const int marginRight = 5;
        private const int marginBottom = 5;
        private const int lineSpacing = 5;
        private readonly SpriteFont font = Game1.smallFont;
        private readonly Vector2 ListHeaderSize;
        private List<String> lines;
        private List<float> lineHeights;
        private List<bool> lineBold;
        private Rectangle bounds;
        public ToDoOverlay(ModEntry theMod, ToDoList theList) {
            this.theMod = theMod;
            this.config = theMod.config.overlay;
            this.theList = theList;
            // save "constant" values
            ListHeaderSize = font.MeasureString(ListHeader);
            // initialize rendering callback
            theMod.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            // initialize the list UI and callback
            theList.OnChanged += OnListChanged;
            syncMenuItemList();
        }

        private void syncMenuItemList() {
            lines = new List<string>();
            lineHeights = new List<float>();
            lineBold = new List<bool>();
            if (theList.Items.Count == 0) return;
            float availableWidth = Math.Max(config.maxWidth - marginLeft - marginRight, ListHeaderSize.X);
            float usedWidth = ListHeaderSize.X;
            float topPx = marginTop + ListHeaderSize.Y;
            foreach (var item in theList.Items) {
                if (item.IsDone || item.HideInOverlay || ! item.IsVisibleToday) continue;
                if (lines.Count >= config.maxItems) {
                    lines.Add("…");
                    float lineHeight = font.MeasureString("…").Y;
                    lineHeights.Add(lineHeight);
                    lineBold.Add(false);
                    topPx += lineHeight;
                    break;
                }
                topPx += lineSpacing;
                string itemText = item.IsHeader ? item.Text : ("  " + item.Text);
                var lineSize = font.MeasureString(itemText);
                while (lineSize.X > availableWidth) {
                    if (itemText.Length < 2) {
                        // this really shouldn't happen
                        break;
                    }
                    itemText = itemText.Remove(itemText.Length - 2) + "…";
                    lineSize = font.MeasureString(itemText);
                }
                usedWidth = Math.Max(usedWidth, lineSize.X);
                lines.Add(itemText);
                lineHeights.Add(lineSize.Y);
                lineBold.Add(item.IsBold);
                topPx += lineSize.Y;
            }
            bounds = new Rectangle(0, 0, (int)(usedWidth + marginLeft + marginRight), (int)topPx + marginBottom);
        }
        private void OnListChanged(object sender, List<ToDoList.ListItem> e) {
            syncMenuItemList();
        }

        public void Dispose() {
            this.theList.OnChanged -= OnListChanged;
            theMod.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        }

        private void OnRenderedWorld(object sender, RenderedWorldEventArgs e) {
            if (lines.Count == 0) return;
            if (!config.enabled) return; // shouldn't get this far, but why not check anyway
            if (Game1.game1.takingMapScreenshot) return;
            if (config.hideAtFestivals && Game1.isFestival()) return;
            var spriteBatch = e.SpriteBatch;
            float topPx = marginTop;
            Rectangle effectiveBounds = bounds;
            if (Game1.CurrentMineLevel > 0 || Game1.currentLocation is VolcanoDungeon vd && vd.level > 0) {
                topPx += 80;
                effectiveBounds.Y += 80;
            }
            spriteBatch.Draw(Game1.fadeToBlackRect, effectiveBounds, config.backgroundColor);
            Utility.drawBoldText(spriteBatch, ListHeader, font, new Vector2(marginLeft, topPx), config.textColor);
            topPx += ListHeaderSize.Y;
            spriteBatch.DrawLine(marginLeft, topPx, new Vector2(ListHeaderSize.X - 3, 1), config.textColor);
            for (int i = 0; i < lines.Count; i++) {
                topPx += lineSpacing;
                if (lineBold[i]) {
                    Utility.drawBoldText(spriteBatch, lines[i], font, new Vector2(marginLeft, topPx), config.textColor);
                } else {
                    spriteBatch.DrawString(font, lines[i], new Vector2(marginLeft, topPx), config.textColor);
                }
                topPx += lineHeights[i];
            }
        }

    }
}
