﻿namespace NadekoBot.Modules.Searches.Commands.Models
{
    class MagicItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public override string ToString() =>
            $"✨`{Name}`\n\t*{Description}*";
    }
}
