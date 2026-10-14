using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SmartRestockFix
{
    /// <summary>
    /// Lembra qual produto pertence a cada slot com auto-restock (ID do slot do
    /// SmartRestock -> ID do item), um arquivo por slot de save. Sem dependencias
    /// do jogo, para poder ser testada.
    /// </summary>
    public class SlotMemory
    {
        private readonly string _directory;
        private Dictionary<string, string> _items = new Dictionary<string, string>();
        private int _saveSlot = int.MinValue;
        private bool _dirty;

        public SlotMemory(string directory)
        {
            _directory = directory;
        }

        public int Count => _items.Count;

        public string FilePath(int saveSlot) => Path.Combine(_directory, $"slot-items-{saveSlot}.json");

        /// <summary>Troca para o save informado, gravando o anterior se houver mudancas.</summary>
        public void UseSaveSlot(int saveSlot)
        {
            if (saveSlot == _saveSlot) return;
            Flush();
            _saveSlot = saveSlot;
            _items = new Dictionary<string, string>();
            var path = FilePath(saveSlot);
            if (!File.Exists(path)) return;
            _items = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                     ?? new Dictionary<string, string>();
        }

        /// <summary>Anota o item do slot. Retorna true se mudou algo.</summary>
        public bool Remember(string slotKey, string itemId)
        {
            if (string.IsNullOrEmpty(slotKey) || string.IsNullOrEmpty(itemId)) return false;
            if (_items.TryGetValue(slotKey, out var current) && current == itemId) return false;
            _items[slotKey] = itemId;
            _dirty = true;
            return true;
        }

        public string Recall(string slotKey) =>
            slotKey != null && _items.TryGetValue(slotKey, out var id) ? id : null;

        /// <summary>Grava no disco se houver mudancas pendentes.</summary>
        public void Flush()
        {
            if (!_dirty || _saveSlot == int.MinValue) return;
            Directory.CreateDirectory(_directory);
            File.WriteAllText(FilePath(_saveSlot),
                JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true }));
            _dirty = false;
        }
    }
}
