// Copyright 2023 Jamie Taylor
using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace ToDew {
    public class ToDoListOverlayDataSource : IToDewOverlayDataSource {
        private readonly ModEntry theMod;
        private ToDoList? _theList;
        private OverlayConfig config { get => theMod.config.overlay; }
        public ToDoList? theList {
            get => _theList;
            set {
                if (_theList is not null) {
                    _theList.OnChanged -= OnListChanged;
                }
                _theList = value;
                if (_theList is not null) {
                    _theList.OnChanged += OnListChanged;
                }
            }
        }
        private readonly Action refreshOverlay;
        public ToDoListOverlayDataSource(ModEntry theMod, Action refreshOverlay) {
            this.theMod = theMod;
            this.refreshOverlay = refreshOverlay;
        }

        public string GetSectionTitle() {
            return I18n.Overlay_Header();
        }

        public List<(string text, bool isBold, Action? onDone)> GetItems(int limit) {
            List<(string text, bool isBold, Action? onDone)> result = new();
            if (theList is null) return result;
            bool lastIsHeader = false;
            foreach (var item in theList.Items) {
                if (item.IsDone || item.HideInOverlay || !item.IsVisibleToday) continue;
                if (!String.IsNullOrWhiteSpace(item.PlayerGsq) &&
                    !GameStateQuery.CheckConditions(item.PlayerGsq)) continue;
                if (item.IsHeader) {
                    if (lastIsHeader && config.hideHeaderWithNoChildren) {
                        result.RemoveAt(result.Count - 1);
                    }
                    lastIsHeader = true;
                    result.Add((item.Text, item.IsBold, null));
                } else {
                    lastIsHeader = false;
                    result.Add(("  " + item.Text, item.IsBold, () => theList.SetItemDone(item, true)));
                    if (result.Count >= limit) break;
                }
            }
            if (config.hideHeaderWithNoChildren && lastIsHeader) {
                result.RemoveAt(result.Count - 1);
            }
            return result;
        }

        private void OnListChanged(object? sender, List<ToDoList.ListItem> e) {
            refreshOverlay();
        }
    }
}

