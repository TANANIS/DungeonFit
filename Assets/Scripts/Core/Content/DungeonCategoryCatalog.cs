using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class DungeonCategoryCatalog
{
    private readonly DungeonCategory[] _categories =
    {
        new("chest", "\u80f8\u5730\u57ce", "\u80f8", "\u80f8\u90e8\u6311\u6230", 108),
        new("shoulders", "\u80a9\u5730\u57ce", "\u80a9", "\u80a9\u90e8\u6311\u6230", 108),
        new("back", "\u80cc\u5730\u57ce", "\u80cc", "\u80cc\u90e8\u6311\u6230", 104),
        new("legs", "\u817f\u5730\u57ce", "\u817f", "\u817f\u90e8\u6311\u6230", 104),
        new("core", "\u6838\u5fc3\u5730\u57ce", "\u6838\u5fc3", "\u6838\u5fc3\u6311\u6230", 100),
        new("arms", "\u624b\u81c2\u5730\u57ce", "\u624b\u81c2", "\u624b\u81c2\u6311\u6230", 108),
    };

    public IReadOnlyList<DungeonCategory> GetAll()
    {
        return _categories;
    }

    public DungeonCategory GetById(string id)
    {
        return _categories.FirstOrDefault(category => category.Id == id) ??
            new DungeonCategory(id, "\u8a13\u7df4\u5730\u57ce", id, "\u8a13\u7df4\u6311\u6230", 108);
    }
}
