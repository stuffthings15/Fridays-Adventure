using System.Drawing;

namespace Fridays_Adventure.Entities
{
    /// <summary>
    /// Defines the effect an item has when collected by the player.
    /// </summary>
    public enum ItemEffect
    {
        /// <summary>Increases the player's score (bounty).</summary>
        Points,
        /// <summary>Restores player health.</summary>
        Health,
        /// <summary>Restores magic (ICE reserve).</summary>
        Magic
    }

    /// <summary>
    /// Base class for all collectible items in the game.
    /// Items can increase points, health, or magic (ICE) when collected.
    /// Subclasses override Draw() to provide unique visuals.
    /// </summary>
    public class Item : Entity
    {
        /// <summary>Whether this item has been collected by the player.</summary>
        public bool Collected { get; set; }

        /// <summary>Point value or restoration amount awarded when collected.</summary>
        public int Value { get; protected set; }

        /// <summary>The type of effect this item applies on collection.</summary>
        public ItemEffect Effect { get; protected set; } = ItemEffect.Points;

        /// <summary>
        /// Creates a new collectible item at the given position.
        /// </summary>
        /// <param name="x">World X position.</param>
        /// <param name="y">World Y position.</param>
        /// <param name="w">Width of the item hitbox.</param>
        /// <param name="h">Height of the item hitbox.</param>
        /// <param name="value">Point/restoration value on collection.</param>
        public Item(float x, float y, int w, int h, int value)
            : base(x, y, w, h)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new collectible item with a specific effect type.
        /// </summary>
        public Item(float x, float y, int w, int h, int value, ItemEffect effect)
            : base(x, y, w, h)
        {
            Value  = value;
            Effect = effect;
        }
    }
}
