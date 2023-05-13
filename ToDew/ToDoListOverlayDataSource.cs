// Copyright 2023 Jamie Taylor
using System;
using System.Collections.Generic;

namespace ToDew {
    public class ToDoListOverlayDataSource : IToDewOverlayDataSource {
        private ToDoList? _theList;
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
        public ToDoListOverlayDataSource(Action refreshOverlay) {
            this.refreshOverlay = refreshOverlay;
        }

        public string GetSectionTitle() {
            return I18n.Overlay_Header();
        }

        public List<(string text, bool isBold, Action? onDone)> GetItems(int limit) {
            List<(string text, bool isBold, Action? onDone)> result = new();
            if (theList is null) return result;
            foreach (var item in theList.Items) {
                if (item.IsDone || item.HideInOverlay || !item.IsVisibleToday) continue;
                string itemText = item.IsHeader ? item.Text : ("  " + item.Text);
                result.Add((itemText, item.IsBold, item.IsHeader ? null : () => theList.SetItemDone(item, true)));
                if (result.Count == limit) break;
            }
            return result;
        }

        private void OnListChanged(object? sender, List<ToDoList.ListItem> e) {
            refreshOverlay();
        }
    }
}

