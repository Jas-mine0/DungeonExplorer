using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Dungeon_Explorer.Program;
using static Dungeon_Explorer.Program.Player;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading;

namespace Dungeon_Explorer
{
    public class Program
    {

        // Design decisions justification (Game class):
        // This is the main entry point for the game, it initalises the game and provides the player with
        // a welcome message.
        // In addition to the above it also deals with showing the player the game menu.
        // I added the game menu so the player has a welcome message to the game.

        public class Game
        {
            public static void Main(string[] args)
            {
                GameInitializer initializer = new GameInitializer();
                Player player = initializer.CreatePlayer();
                GameMap gameMap = initializer.CreateGameMap();
                Statistics stats = new Statistics();

                stats.TrackPlayer(player);

                Console.WriteLine("=================================");
                Console.WriteLine("       DUNGEON EXPLORER    ");
                Console.WriteLine("=================================");
                Console.WriteLine("Press any key to start your adventure.");
                Console.ReadKey();

                GameMenu gameMenu = new GameMenu(player, gameMap, stats);
                gameMenu.ShowMenu();
            }
        }

        // Design decisions justification:
        // This class encapsulates the logic for the game components.
        // I have decided to give the player some starting items in the game
        // so they can start getting used to playing the game.
        // The code has been put in this class so the class above does not get too cluttered.

        public class GameInitializer
        {
            public Player CreatePlayer()
            {
                Player player = new Player("Default", 100, 15, 8);

                player.AddToInventory(new Weapons("Rusty Sword", "An old but reliable blade", 8));
                player.AddToInventory(new Potions("Health Potion", "Restores 25 health points", 25));
                player.AddToInventory(new Key("Bronze Key", "Opens a common lock", KeyType.Bronze));

                return player;
            }

            public GameMap CreateGameMap()
            {
                GameMap gameMap = new GameMap();
                gameMap.LoadGameMap();
                return gameMap;
            }
        }

        public class Statistics
        {
            private Player _player;
            private int _monstersDefeated;
            private int _roomsVisited;
            private int _itemsCollected;
            private DateTime _startTime;
            private int _score;

            public Statistics()
            {
                _monstersDefeated = 0;
                _roomsVisited = 0;
                _itemsCollected = 0;
                _startTime = DateTime.Now;
                _score = 0;
            }

            public void TrackPlayer(Player player) => _player = player;

            public void MonsterDefeated(int experienceValue)
            {
                _monstersDefeated++;
                UpdateScore(experienceValue * 10);
            }

            public void RoomVisited()
            {
                _roomsVisited++;
                UpdateScore(5);
            }

            public void ItemCollected()
            {
                _itemsCollected++;
                UpdateScore(2);
            }

            private void UpdateScore(int points) => _score += points;

            public int GetLevel() => (_player?.Experience ?? 0) / 50 + 1;

            public void DisplayStatistics()
            {
                TimeSpan playTime = DateTime.Now - _startTime;

                Console.WriteLine("==================================");
                Console.WriteLine("         GAME STATISTICS          ");
                Console.WriteLine("==================================");
                Console.WriteLine($"Player Level: {GetLevel()}");
                Console.WriteLine($"Monsters Defeated: {_monstersDefeated}");
                Console.WriteLine($"Rooms Visited: {_roomsVisited}");
                Console.WriteLine($"Items Collected: {_itemsCollected}");
                Console.WriteLine($"Play Time: {playTime.Hours:D2}:{playTime.Minutes:D2}:{playTime.Seconds:D2}");
                Console.WriteLine($"Score: {_score}");
                Console.WriteLine("==================================");
            }
        }

        // Design decisions justification (Creature):
        // The "Creature" class is abstract as it will allow the class to be used for inheritance.
        // This is because the "abstract" modifier indicates that it is supposed to be a base class
        // for other classes.
        // The class also implements "IDamageable" which deals with consistent damage handling.
        // The class makes use of the keyword "Virtual" so the method can be overidden.
        // In addition to this, the class also makes use of the keyword "protected" to ensure that pieces of
        // code are only accessed by code in the current class (or a class derived from the current one).

        public abstract class Creature : IDamageable
        {
            protected string _name;
            protected int _health;
            protected int _maxHealth;
            protected int _attackPower;
            protected int _defense;
            protected bool _isAlive;

            public string Name { get => _name; protected set => _name = value; }
            public int MaxHealth { get => _maxHealth; protected set => _maxHealth = value; }
            public int CurrentHealth
            {
                get => _health;
                set
                {
                    _health = Math.Max(0, Math.Min(value, _maxHealth));
                    if (_health <= 0)
                        _isAlive = false;
                }
            }
            public int AttackPower { get => _attackPower; set => _attackPower = value; }
            public int Defense { get => _defense; protected set => _defense = value; }
            public bool IsAlive { get => _isAlive; protected set => _isAlive = value; }

            protected Creature(string name, int maxHealth, int attackPower, int defense)
            {
                _name = name;
                _maxHealth = maxHealth;
                _health = maxHealth;
                _attackPower = attackPower;
                _defense = defense;
                _isAlive = true;
            }

            public virtual void DamageTaken(int amount)
            {
                int actualDamageTaken = Math.Max(1, amount - _defense);
                CurrentHealth -= actualDamageTaken;
                Console.WriteLine($"{_name} has taken {actualDamageTaken} damage. Health: {_health}/{_maxHealth}");
            }

            public abstract void Attack(IDamageable target);

            public virtual void Heal(int amount)
            {
                if (!_isAlive)
                {
                    Console.WriteLine($"{_name} is defeated and cannot be healed.");
                    return;
                }

                int previousHealth = _health;
                CurrentHealth += amount;
                Console.WriteLine($"{_name} heals for {_health - previousHealth} points. Health: {_health}/{MaxHealth}");
            }

            protected bool IsValidTarget(IDamageable target)
            {
                if (target == null)
                {
                    Console.WriteLine("There is no target to attack!");
                    return false;
                }

                if (!target.IsAlive)
                {
                    Console.WriteLine("The target is already defeated.");
                    return false;
                }

                if (!_isAlive)
                {
                    Console.WriteLine("You cannot attack while defeated.");
                    return false;
                }

                return true;
            }

            public override string ToString() => $"{_name} - Health: {_health}/{_maxHealth}, Attack: {_attackPower}, Defense: {_defense}";
        }

        // Design decisions justification (Player Class):
        // This class deals with inventory management, allowing the player to add, use, or discard items
        // in their inventory.
        // The code also makes use of LINQ so the player can query their inventory items.

        public class Player : Creature
        {
            public string playerName;
            private List<Items> _inventory;
            private Weapons _equippedWeapon;
            private int _gold;
            private int _experience;
            private int _inventoryCapacity;

            public List<Items> Inventory => _inventory;
            public Weapons EquippedWeapon => _equippedWeapon;
            public int Gold { get => _gold; set => _gold = value; }
            public int Experience { get => _experience; set => _experience = value; }
            public bool SkipNextTurn { get; set; } = false;
            public int InventoryCapacity { get => _inventoryCapacity; set => _inventoryCapacity = Math.Max(1, value); }

            public Player(string name, int maxHealth, int attackPower, int defense)
                    : base(name, maxHealth, attackPower, defense)
            {
                _inventory = new List<Items>();
                _gold = 0;
                _experience = 0;
                _inventoryCapacity = 10;
                GetPlayerName();
            }

            public Player(string name, int maxHealth, int attackPower, int defense, int inventoryCapacity)
                    : this(name, maxHealth, attackPower, defense)
            {
                _inventoryCapacity = inventoryCapacity;
            }

            private void GetPlayerName()
            {
                while (true)
                {
                    Console.WriteLine("Please enter your name: ");
                    playerName = Console.ReadLine();
                    if (string.IsNullOrEmpty(playerName))
                    {
                        Console.WriteLine("You can't have an empty name. Please enter a name");
                    }
                    else
                    {
                        Console.WriteLine($"Hello, {playerName}");
                        _name = playerName;
                        Console.WriteLine($"Your health is: {CurrentHealth}/{MaxHealth}");
                        break;
                    }
                }
            }

            public override void Attack(IDamageable target)
            {
                if (!IsValidTarget(target)) return;

                int damageAmount = _attackPower;
                if (_equippedWeapon != null)
                {
                    damageAmount += _equippedWeapon.Damage;
                }
                Console.WriteLine($"{_name} attacks with {(_equippedWeapon != null ? _equippedWeapon.Name : "bare hands")} for {damageAmount} damage");
                target.DamageTaken(damageAmount);
            }

            public void RoomSelection(GameMap gameMap)
            {
                List<int> availableRooms = gameMap.GetAvailableRooms();

                Console.WriteLine("Available rooms to explore:");
                foreach (int roomId in availableRooms)
                {
                    Room room = gameMap.GetRoom(roomId);
                    if (room != null)
                    {
                        Console.WriteLine($"{roomId}. {room.Name}" + (gameMap.GetCurrentRoom().IsConnectionLocked(roomId) ? " (Locked)" : ""));
                    }
                }

                Console.WriteLine("Pick a room to go into: (enter room number)");
                string input = Console.ReadLine();
                if (int.TryParse(input, out int roomNumber))
                {
                    if (availableRooms.Contains(roomNumber))
                    {
                        gameMap.MoveToAnotherRoom(roomNumber, this);
                    }
                    else
                    {
                        Console.WriteLine("That room is not accessible from here.");
                    }
                }
                else
                {
                    Console.WriteLine("Please enter a valid room number.");
                }
            }

            public void EquipWeapon(Weapons weapon)
            {
                if (weapon == null)
                {
                    Console.WriteLine("No weapon to equip.");
                    return;
                }

                _equippedWeapon = weapon;
                Console.WriteLine($"{_name} equipped {weapon.Name}");
            }

            public void EquipStrongestWeapon()
            {
                var strongestWeapon = _inventory
                    .Where(item => item is Weapons)
                    .Cast<Weapons>()
                    .OrderByDescending(w => w.Damage)
                    .FirstOrDefault();

                if (strongestWeapon != null)
                {
                    EquipWeapon(strongestWeapon);
                }
                else
                {
                    Console.WriteLine("You don't have any weapons to equip.");
                }
            }

            public bool AddToInventory(Items item)
            {
                if (_inventory.Count >= _inventoryCapacity)
                {
                    Console.WriteLine($"Your inventory is full. (Limit: {_inventoryCapacity} items)");
                    Console.WriteLine("Would you like to discard an item to make space? (y/n)");
                    string response = Console.ReadLine().ToLower();

                    if (response == "y" || response == "yes")
                    {
                        if (DiscardItem())
                        {
                            _inventory.Add(item);
                            Console.WriteLine($"{item.Name} added to inventory.");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("No item discarded. Cannot add new item.");
                            return false;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Item not added to inventory.");
                        return false;
                    }
                }

                _inventory.Add(item);
                Console.WriteLine($"{item.Name} added to inventory.");
                return true;
            }

            private bool DiscardItem()
            {
                Console.WriteLine("Select an item to discard:");
                ShowInventory();

                Console.WriteLine("Enter the number of the item to discard (0 to cancel):");
                if (int.TryParse(Console.ReadLine(), out int choice))
                {
                    if (choice == 0)
                    {
                        return false;
                    }

                    if (choice > 0 && choice <= _inventory.Count)
                    {
                        Items itemToRemove = _inventory[choice - 1];
                        _inventory.RemoveAt(choice - 1);
                        Console.WriteLine($"Discarded {itemToRemove.Name}.");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Invalid item number.");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input. No item discarded.");
                    return false;
                }
            }

            public void UsePotion(int inventoryIndex)
            {
                if (inventoryIndex < 0 || inventoryIndex >= _inventory.Count)
                {
                    Console.WriteLine("Invalid inventory index.");
                    return;
                }

                Items item = _inventory[inventoryIndex];
                if (item is Potions potion)
                {
                    potion.Use(this);
                    _inventory.RemoveAt(inventoryIndex);
                }
                else
                {
                    Console.WriteLine("That item is not a potion!");
                }
            }

            public bool UseItem(int inventoryIndex)
            {
                if (inventoryIndex < 0 || inventoryIndex >= _inventory.Count)
                {
                    Console.WriteLine("Invalid inventory index.");
                    return false;
                }

                Items item = _inventory[inventoryIndex];
                if (item is IUsable usableItem)
                {
                    bool result = usableItem.Use(this);
                    if (result && usableItem.IsConsumed)
                    {
                        _inventory.RemoveAt(inventoryIndex);
                    }
                    return result;
                }
                else if (item is Weapons weapon)
                {
                    EquipWeapon(weapon);
                    return true;
                }
                else
                {
                    Console.WriteLine($"You can't use {item.Name} in this way.");
                    return false;
                }
            }

            public void CollectItem(ICollectable item)
            {
                if (item == null)
                {
                    Console.WriteLine("There is nothing to collect.");
                    return;
                }

                if (item.CanBeCollected)
                {
                    item.OnCollection(this);
                }
                else
                {
                    Console.WriteLine($"You cannot collect {item.Name} at this time.");
                }
            }

            public void ShowInventory(Func<Items, bool> filter = null, Comparison<Items> sortComparison = null)
            {
                if (_inventory.Count == 0)
                {
                    Console.WriteLine("Your inventory is empty");
                    return;
                }

                List<Items> itemsToShow = new List<Items>(_inventory);

                if (filter != null)
                {
                    itemsToShow = itemsToShow.Where(filter).ToList();
                    if (itemsToShow.Count == 0)
                    {
                        Console.WriteLine("No items match the filter criteria.");
                        return;
                    }
                }

                if (sortComparison != null)
                {
                    itemsToShow.Sort(sortComparison);
                }

                Console.WriteLine($"Inventory ({itemsToShow.Count}/{_inventoryCapacity} slots used):");
                for (int i = 0; i < itemsToShow.Count; i++)
                {
                    DisplayItemDetails(i + 1, itemsToShow[i]);
                }
            }

            private void DisplayItemDetails(int index, Items item)
            {
                Console.WriteLine($"{index}. {item.Name} - {item.Description}");

                if (item is Potions potion)
                {
                    Console.WriteLine($"   Healing: +{potion.HealAmount} HP");
                }
                else if (item is Weapons weapon)
                {
                    Console.WriteLine($"   Damage: +{weapon.Damage}" + (weapon == _equippedWeapon ? " (Equipped)" : ""));
                }
                else if (item is Key key)
                {
                    Console.WriteLine($"   Type: {key.KeyType} Key");
                }
            }

            public List<Potions> GetAllHealingItems()
            {
                return _inventory
                    .Where(item => item is Potions)
                    .Cast<Potions>()
                    .OrderByDescending(p => p.HealAmount)
                    .ToList();
            }

            public bool HasKeyOfType(KeyType keyType)
            {
                return _inventory.Any(item => item is Key key && key.KeyType == keyType);
            }

            public void UseKey(KeyType keyType)
            {
                for (int i = 0; i < _inventory.Count; i++)
                {
                    if (_inventory[i] is Key key && key.KeyType == keyType)
                    {
                        Console.WriteLine($"Used {key.Name} to unlock the door.");
                        _inventory.RemoveAt(i);
                        return;
                    }
                }
            }

            public void DisplayExperience()
            {
                int level = Experience / 50 + 1;
                int expToNextLevel = (level * 50) - Experience;

                Console.WriteLine($"Experience: {Experience} (Level {level})");
                Console.WriteLine($"Experience needed for next level: {expToNextLevel}");
            }
        }

        public interface IUsable
        {
            bool Use(Creature target);
            bool IsConsumed { get; }
        }

        // Design decisions justification (Items class):
        // This class uses the "ICollectable" interface.
        // The use of an interface instead of inheritance is because it is an "is a" relationship.
        // In addition to this, using an interface instead of inheritance allows me to use more than one
        // interface if I need to whereas if I used inheritance then I would only be able to inherit from
        // one class.
        // "ICollectable" allows the player to collect items consistently.

        public abstract class Items : ICollectable
        {
            protected string _name;
            protected string _description;
            protected bool _canBeCollected = true;

            public string Name { get => _name; protected set => _name = value; }
            public string Description { get => _description; protected set => _description = value; }
            public bool CanBeCollected { get => _canBeCollected; protected set => _canBeCollected = value; }

            protected Items(string name, string description)
            {
                _name = name;
                _description = description;
            }

            public abstract void OnCollection(Player collector);

            public override string ToString() => $"{_name}: {_description}";
        }

        public class Weapons : Items
        {
            private int _damage;
            public int Damage => _damage;

            public Weapons(string name, string description, int damage) : base(name, description)
            {
                _damage = damage;
            }

            public override void OnCollection(Player collector)
            {
                Console.WriteLine($"{collector.Name} collected {_name}");
                collector.AddToInventory(this);
            }
        }

        public class Potions : Items, IUsable
        {
            private int _healAmount;
            public int HealAmount => _healAmount;
            public bool IsConsumed => true;

            public Potions(string name, string description, int healAmount) : base(name, description)
            {
                _healAmount = healAmount;
            }

            public override void OnCollection(Player collector)
            {
                Console.WriteLine($"{collector.Name} collected {_name}");
                collector.AddToInventory(this);
            }

            public bool Use(Creature target)
            {
                Console.WriteLine($"Using {_name} on {target.Name}");
                target.Heal(_healAmount);
                _canBeCollected = false;
                return true;
            }
        }

        public enum KeyType
        {
            Bronze,
            Silver,
            Gold,
            Crystal
        }

        public class Key : Items, IUsable
        {
            private KeyType _keyType;
            public KeyType KeyType => _keyType;
            public bool IsConsumed => true;

            public Key(string name, string description, KeyType keyType) : base(name, description)
            {
                _keyType = keyType;
            }

            public override void OnCollection(Player collector)
            {
                Console.WriteLine($"{collector.Name} collected {_name}");
                collector.AddToInventory(this);
            }

            public bool Use(Creature target)
            {
                if (target is Player player)
                {
                    Console.WriteLine($"{player.Name} tries to use {_name} but there's no lock here.");
                    return false;
                }
                return false;
            }
        }

        public interface IDamageable
        {
            void DamageTaken(int amount);
            bool IsAlive { get; }
            int CurrentHealth { get; }
            string Name { get; }
        }

        public interface ICollectable
        {
            string Name { get; }
            string Description { get; }
            bool CanBeCollected { get; }
            void OnCollection(Player collector);
        }

        // Design decisions justification (Monster Class):
        // The code makes use of inheritance by using the base class "Creature".
        // This means that the "Monster" class inherits the same fields and methods that are in the
        // "Creature" base class.
        // I have used polymorphism within the class so each monster that inherits from the base class
        // "Monster" can do different things.
        // Each monster inherits from the base class "Monster" so that 
        // The "DecideBehaviour" allows the monsters to change their tactics.

        public abstract class Monster : Creature
        {
            protected string _description;
            protected List<Items> _loot;
            protected int _experienceValue;
            protected MonsterBehaviour _behaviour;

            public string Description => _description;
            public int ExperienceValue { get => _experienceValue; set => _experienceValue = value; }
            public MonsterBehaviour Behaviour => _behaviour;

            protected Monster(string name, string description, int maxHealth, int attackPower, int defense, int experienceValue)
                : base(name, maxHealth, attackPower, defense)
            {
                _description = description;
                _loot = new List<Items>();
                _experienceValue = experienceValue;
                _behaviour = MonsterBehaviour.Aggressive;
            }

            public override void Attack(IDamageable target)
            {
                if (!IsValidTarget(target)) return;

                Console.WriteLine($"{_name} attacks {target.Name} for {_attackPower} damage");
                target.DamageTaken(_attackPower);
            }

            public void AddLoot(Items item)
            {
                _loot.Add(item);
            }

            public List<Items> DropLoot()
            {
                if (_isAlive)
                {
                    Console.WriteLine($"{_name} is still alive and won't drop loot.");
                    return new List<Items>();
                }

                Console.WriteLine($"{_name} drops loot.");
                List<Items> droppedItems = new List<Items>(_loot);
                _loot.Clear();
                return droppedItems;
            }

            public virtual MonsterBehaviour DecideBehavior()
            {
                double healthPercentage = (double)CurrentHealth / MaxHealth;

                if (healthPercentage < 0.2)
                {
                    return MonsterBehaviour.Fleeing;
                }
                else if (healthPercentage < 0.5)
                {
                    return MonsterBehaviour.Defensive;
                }
                else
                {
                    return MonsterBehaviour.Aggressive;
                }
            }
        }

        public enum MonsterBehaviour
        {
            Aggressive,
            Defensive,
            Fleeing
        }

        public class FrogMonster : Monster
        {
            public FrogMonster()
                : base("Frog monster", "You have encountered an oversized frog.", 20, 5, 2, 10)
            {
                AddDefaultLoot();
            }

            private void AddDefaultLoot()
            {
                AddLoot(new Potions("Frog Potion", "A strange potion made from frog essence", 15));
            }

            public override void Attack(IDamageable target)
            {
                if (!IsValidTarget(target)) return;

                Console.WriteLine($"{_name} leaps at {target.Name} and attacks with its tongue for {_attackPower} damage");
                target.DamageTaken(_attackPower);
            }
        }

        public class GnomeMonster : Monster
        {
            public GnomeMonster()
                : base("Gnome", "You have encountered a mischievous gnome.", 25, 6, 4, 12)
            {
                AddDefaultLoot();
            }

            private void AddDefaultLoot()
            {
                AddLoot(new Weapons("Gardening Shovel", "A tiny but effective tool", 8));
            }

            public override void Attack(IDamageable target)
            {
                if (!IsValidTarget(target)) return;

                Console.WriteLine($"{_name} drops a plant pot on {target.Name}, dealing {_attackPower} damage");
                target.DamageTaken(_attackPower);
            }

            public override MonsterBehaviour DecideBehavior()
            {
                double healthPercentage = (double)CurrentHealth / MaxHealth;

                if (healthPercentage < 0.4)
                {
                    return MonsterBehaviour.Fleeing;
                }
                else
                {
                    return MonsterBehaviour.Aggressive;
                }
            }
        }

        public class SeagullMonster : Monster
        {
            public SeagullMonster()
                : base("Seagull", "You have encountered an angry giant seagull.", 30, 7, 2, 13)
            {
                AddDefaultLoot();
            }

            private void AddDefaultLoot()
            {
                AddLoot(new Potions("Feather Essence", "A potion made from magical feathers", 18));
                AddLoot(new Key("Bronze Key", "A key the seagull had been carrying", KeyType.Bronze));
            }

            public override void Attack(IDamageable target)
            {
                if (!IsValidTarget(target)) return;

                Console.WriteLine($"{_name} pecks at {target.Name} aggressively, dealing {_attackPower} damage");
                target.DamageTaken(_attackPower);
            }
        }

        public class BossMonster : Monster
        {
            private bool _isActive;
            private int _specialAttackCooldown = 0;

            public bool IsActive { get => _isActive; set => _isActive = value; }

            public BossMonster()
                : base("Ancient Guardian", "A massive stone guardian awakens before you.", 75, 15, 10, 50)
            {
                _isActive = false;
                AddDefaultLoot();
            }

            private void AddDefaultLoot()
            {
                AddLoot(new Weapons("Guardian's Hammer", "An ancient weapon of immense power", 20));
                AddLoot(new Potions("Elixir of Life", "A legendary healing potion", 50));
                AddLoot(new Key("Crystal Key", "The key to the castle's treasure room", KeyType.Crystal));
            }

            public override void Attack(IDamageable target)
            {
                if (!_isActive || !IsValidTarget(target)) return;

                if (_specialAttackCooldown <= 0)
                {
                    Console.WriteLine($"{_name} charges up and unleashes a devastating slam attack on {target.Name}, dealing {_attackPower * 2} damage.");
                    target.DamageTaken(_attackPower * 2);
                    _specialAttackCooldown = 3;
                }
                else
                {
                    Console.WriteLine($"{_name} swings its massive fist at {target.Name}, dealing {_attackPower} damage");
                    target.DamageTaken(_attackPower);
                    _specialAttackCooldown--;
                }
            }

            public void Activate()
            {
                if (_isActive) return;

                _isActive = true;
                Console.WriteLine("The ancient guardian's eyes glow with ethereal light as it rises to defend its domain.");
            }

            public override MonsterBehaviour DecideBehavior()
            {
                double healthPercentage = (double)CurrentHealth / MaxHealth;

                if (healthPercentage < 0.3)
                {
                    Console.WriteLine($"{_name} enters a berserker rage as its health dwindles.");
                }

                return MonsterBehaviour.Aggressive;
            }
        }

        // Design decisions justification (Room Base Class):
        // This class keeps track of the ID, descriptions, items, monsters, connected rooms, and the required
        // keys.
        // In addition to keeping track of whether the room has been visted before and the name of the room.
        // Furthermore, it tells the player if a room is locked and if they can pick up an item or not.
        // The class also makes use of encapsulation, virtual methods, and public/private access modifiers.
        // The use of virtual methods allowing it to be overriden if it needs to be overriden.

        public class Room
        {
            private int _id;
            private string _description;
            private List<Items> _items;
            private Monster _monster;
            private Dictionary<int, bool> _connectedRooms;
            private Dictionary<int, KeyType> _requiredKeys;
            private bool _hasBeenVisited;
            private string _name;

            public int ID => _id;
            public string Description => _description;
            public List<Items> Items => _items;
            public Monster Monster => _monster;
            public bool HasBeenVisited { get => _hasBeenVisited; set => _hasBeenVisited = value; }
            public string Name => _name;

            public Room(int id, string name, string description)
            {
                _id = id;
                _name = name;
                _description = description;
                _items = new List<Items>();
                _monster = null;
                _connectedRooms = new Dictionary<int, bool>();
                _requiredKeys = new Dictionary<int, KeyType>();
                _hasBeenVisited = false;
            }

            public void SetMonster(Monster monster)
            {
                _monster = monster;
            }

            public void AddItem(Items item)
            {
                _items.Add(item);
            }

            public void ConnectRoom(int roomId)
            {
                _connectedRooms[roomId] = false;
            }

            public void ConnectLockedRoom(int roomId, KeyType keyType)
            {
                _connectedRooms[roomId] = true;
                _requiredKeys[roomId] = keyType;
            }

            public List<int> GetConnectedRooms()
            {
                return _connectedRooms.Keys.ToList();
            }

            public bool IsConnectionLocked(int roomId)
            {
                return _connectedRooms.ContainsKey(roomId) && _connectedRooms[roomId];
            }

            public KeyType GetRequiredKeyType(int roomId)
            {
                return _requiredKeys.ContainsKey(roomId) ? _requiredKeys[roomId] : KeyType.Bronze;
            }

            public virtual void DisplayRoom()
            {
                Console.WriteLine($"==== {_name} ====");
                Console.WriteLine(_description);

                if (_monster != null && _monster.IsAlive)
                {
                    Console.WriteLine($"\nMonster: {_monster.Name} - {_monster.Description}");
                }

                DisplayItems();

                Console.WriteLine("\nExits:");
                foreach (var roomId in _connectedRooms.Keys)
                {
                    Console.Write($"Room {roomId}" + (_connectedRooms[roomId] ? " (Locked)" : ""));
                    Console.WriteLine();
                }
            }

            public void DisplayItems()
            {
                if (_items.Count > 0)
                {
                    Console.WriteLine("\nItems in this room:");
                    for (int i = 0; i < _items.Count; i++)
                    {
                        Console.WriteLine($"{i + 1}. {_items[i].Name} - {_items[i].Description}");
                    }
                }
                else
                {
                    Console.WriteLine("\nThere are no items in this room.");
                }
            }

            public void CollectItem(int index, Player player)
            {
                if (_items.Count == 0)
                {
                    Console.WriteLine("There are no items to collect in this room.");
                    return;
                }

                if (index < 0 || index >= _items.Count)
                {
                    Console.WriteLine("Invalid item selection.");
                    return;
                }

                Items item = _items[index];
                if (player.AddToInventory(item))
                {
                    _items.RemoveAt(index);
                }
            }

            public void GenerateRandomEncounter()
            {
                if (_monster != null && _monster.IsAlive) return;

                Random random = new Random();
                int encounterChance = random.Next(100);

                if (encounterChance < 30)
                {
                    int monsterType = random.Next(5);

                    switch (monsterType)
                    {
                        case 0:
                            _monster = new FrogMonster();
                            break;
                        case 1:
                            _monster = new GnomeMonster();
                            break;
                        case 2:
                            _monster = new SeagullMonster();
                            break;
                        default:
                            _monster = new FrogMonster();
                            break;
                    }

                    Console.WriteLine($"As you explore the room, a {_monster.Name} appears.");
                }
            }
        }

        public abstract class SpecialRoom : Room
        {
            protected bool _puzzleSolved;
            protected Items _rewardItem;

            public bool PuzzleSolved => _puzzleSolved;

            public SpecialRoom(int id, string name, string description)
                : base(id, name, description)
            {
                _puzzleSolved = false;
            }

            public void SetReward(Items item)
            {
                _rewardItem = item;
            }

            protected void OnPuzzleSolved(Player player)
            {
                if (_puzzleSolved) return;

                _puzzleSolved = true;
                Console.WriteLine("Congratulations! You solved the puzzle.");

                if (_rewardItem != null)
                {
                    Console.WriteLine($"You found a {_rewardItem.Name}!");
                    player.AddToInventory(_rewardItem);
                }
            }

            public abstract bool AttemptPuzzle(Player player, string attempt);

            public override void DisplayRoom()
            {
                base.DisplayRoom();

                if (_puzzleSolved)
                {
                    Console.WriteLine("\nPuzzle Status: SOLVED");
                }
                else
                {
                    Console.WriteLine("\nPuzzle Status: UNSOLVED");
                    DisplayPuzzle();
                }
            }

            protected abstract void DisplayPuzzle();
        }

        public class RiddleRoom : SpecialRoom
        {
            private Dictionary<string, string> _riddles;
            private string _currentRiddle;
            private string _currentAnswer;
            private int _attempts;

            public RiddleRoom(int id, string name, string description)
                : base(id, name, description)
            {
                _attempts = 0;
                InitializeRiddles();
                SelectRandomRiddle();
            }

            private void InitializeRiddles()
            {
                _riddles = new Dictionary<string, string>
            {
                { "I speak without a mouth and hear without ears. I have no body, but I come alive with wind. What am I?", "echo" },
                { "The more you take, the more you leave behind. What am I?", "footsteps" },
                { "What has keys but no locks, space but no room, and you can enter but not go in?", "keyboard" },
                { "What has a head, a tail, is brown, and has no legs?", "penny" },
                { "What goes up but never comes down?", "age" }
            };
            }

            private void SelectRandomRiddle()
            {
                List<string> riddleList = new List<string>(_riddles.Keys);
                Random random = new Random();
                int index = random.Next(riddleList.Count);
                _currentRiddle = riddleList[index];
                _currentAnswer = _riddles[_currentRiddle];
            }

            protected override void DisplayPuzzle()
            {
                Console.WriteLine("\n=== THE RIDDLE CHALLENGE ===");
                Console.WriteLine("Solve this riddle to progress:");
                Console.WriteLine($"Riddle: {_currentRiddle}");
                Console.WriteLine($"Attempts made: {_attempts}/3");
                Console.WriteLine("\nTo attempt an answer, type the answer");
            }

            public override bool AttemptPuzzle(Player player, string attempt)
            {
                if (_puzzleSolved)
                {
                    Console.WriteLine("This puzzle has already been solved.");
                    return true;
                }

                attempt = attempt.ToLower().Trim();
                _attempts++;

                if (attempt == _currentAnswer)
                {
                    OnPuzzleSolved(player);
                    return true;
                }
                else
                {
                    Console.WriteLine("That's not the correct answer.");

                    if (_attempts >= 3)
                    {
                        Console.WriteLine("You've used all your attempts. Here's a hint:");
                        Console.WriteLine($"The first letter is '{_currentAnswer[0]}' and it has {_currentAnswer.Length} letters.");
                    }
                    else
                    {
                        Console.WriteLine($"You have {3 - _attempts} attempts remaining.");
                    }

                    return false;
                }
            }
        }

        public class MemoryPuzzleRoom : SpecialRoom
        {
            private List<string> _sequence;
            private int _currentLevel;
            private readonly int _maxLevel = 3;

            public MemoryPuzzleRoom(int id, string name, string description)
                : base(id, name, description)
            {
                _sequence = new List<string>();
                _currentLevel = 1;
                GenerateSequence();
            }

            private void GenerateSequence()
            {
                _sequence.Clear();
                Random random = new Random();
                string[] colors = { "Red", "Blue", "Green", "Yellow" };

                for (int i = 0; i < 2 + _currentLevel; i++)
                {
                    _sequence.Add(colors[random.Next(colors.Length)]);
                }
            }

            protected override void DisplayPuzzle()
            {
                Console.WriteLine("\n=== THE MEMORY CHALLENGE ===");
                Console.WriteLine($"Level {_currentLevel} of {_maxLevel}");
                Console.WriteLine("Memorize the sequence of colors that will be shown.");
                Console.WriteLine("Type 'start' to begin the sequence display, then repeat the sequence when prompted.");
                Console.WriteLine("To answer, type the colors separated by spaces (e.g., 'Red Blue Green')");
            }

            public void DisplaySequence()
            {
                Console.WriteLine("\nWatch carefully...");
                Thread.Sleep(1000);

                foreach (string color in _sequence)
                {
                    Console.Clear();
                    Console.WriteLine($"=== {color.ToUpper()} ===");
                    Thread.Sleep(1000);
                }

                Console.Clear();
                Console.WriteLine("Now repeat the sequence:");
            }

            public override bool AttemptPuzzle(Player player, string attempt)
            {
                if (_puzzleSolved)
                {
                    Console.WriteLine("This puzzle has already been solved.");
                    return true;
                }

                if (attempt.ToLower() == "start")
                {
                    DisplaySequence();
                    return false;
                }

                string[] inputColors = attempt.Split(' ');

                if (inputColors.Length != _sequence.Count)
                {
                    Console.WriteLine("The number of colors you entered doesn't match the sequence length.");
                    return false;
                }

                bool correct = true;
                for (int i = 0; i < _sequence.Count; i++)
                {
                    if (inputColors[i].ToLower() != _sequence[i].ToLower())
                    {
                        correct = false;
                        break;
                    }
                }

                if (correct)
                {
                    Console.WriteLine("That's correct!");

                    if (_currentLevel >= _maxLevel)
                    {
                        OnPuzzleSolved(player);
                        return true;
                    }
                    else
                    {
                        _currentLevel++;
                        Console.WriteLine($"Moving to level {_currentLevel}...");
                        GenerateSequence();
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("That's not correct. The sequence was:");
                    Console.WriteLine(string.Join(" ", _sequence));
                    Console.WriteLine("Let's try again with a new sequence.");
                    GenerateSequence();
                    return false;
                }
            }
        }

        public class ChessPuzzleRoom : SpecialRoom
        {
            private char[,] _board;
            private int _movesRemaining;
            private List<string> _validMoves;

            public ChessPuzzleRoom(int id, string name, string description)
                : base(id, name, description)
            {
                InitializeBoard();
                _movesRemaining = 3;
                _validMoves = GetValidMoves();
            }

            private void InitializeBoard()
            {
                _board = new char[5, 5]
                {
                { ' ', ' ', ' ', ' ', ' ' },
                { ' ', ' ', 'P', ' ', ' ' }, // P = player pawn
                { ' ', 'R', ' ', 'B', ' ' }, // R = rook, B = bishop
                { ' ', ' ', 'K', ' ', ' ' }, // K = king
                { ' ', ' ', 'X', ' ', ' ' }  // X = target position
                };
            }

            private List<string> GetValidMoves()
            {
                return new List<string>
            {
                "Kc2", // Move king to c2
                "Pb3", // Move pawn to b3
                "Rd1"  // Move rook to d1
            };
            }

            protected override void DisplayPuzzle()
            {
                Console.WriteLine("\n=== THE CHESS PUZZLE ===");
                Console.WriteLine("Move the pieces to checkmate the opponent's king in exactly 3 moves.");
                Console.WriteLine("The board is represented as follows:");
                Console.WriteLine("P = Pawn, R = Rook, B = Bishop, K = King, X = Target");
                Console.WriteLine("Use algebraic notation to move pieces: [Piece][destination]");
                Console.WriteLine("Example: 'Kc2' to move King to position c2");
                Console.WriteLine($"Moves remaining: {_movesRemaining}");

                Console.WriteLine("\n  a b c d e");
                Console.WriteLine("  ─────────");
                for (int i = 0; i < 5; i++)
                {
                    Console.Write($"{5 - i}│");
                    for (int j = 0; j < 5; j++)
                    {
                        Console.Write(_board[i, j] + " ");
                    }
                    Console.WriteLine();
                }
            }

            public override bool AttemptPuzzle(Player player, string attempt)
            {
                if (_puzzleSolved)
                {
                    Console.WriteLine("This puzzle has already been solved.");
                    return true;
                }

                attempt = attempt.Trim();

                if (_validMoves.Contains(attempt))
                {
                    char piece = attempt[0];
                    string destination = attempt.Substring(1);

                    Console.WriteLine($"You moved {GetPieceName(piece)} to {destination}.");
                    _validMoves.Remove(attempt);
                    _movesRemaining--;

                    if (_movesRemaining == 0 || _validMoves.Count == 0)
                    {
                        OnPuzzleSolved(player);
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"That move is correct. {_movesRemaining} moves remaining.");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("That's not a valid move for this puzzle.");

                    if (_movesRemaining == 1)
                    {
                        Console.WriteLine("Hint: The final move involves the Rook.");
                    }

                    return false;
                }
            }

            private string GetPieceName(char pieceSymbol)
            {
                switch (pieceSymbol)
                {
                    case 'P': return "Pawn";
                    case 'R': return "Rook";
                    case 'B': return "Bishop";
                    case 'K': return "King";
                    default: return "Unknown Piece";
                }
            }
        }

        public class GameMap
        {
            private Dictionary<int, Room> _rooms;
            private int _currentRoomId;
            private Statistics _stats;

            public GameMap()
            {
                _rooms = new Dictionary<int, Room>();
                _currentRoomId = 1;
                _stats = null;
            }

            public void DebugPrintRoomConnections()
            {
                Console.WriteLine("===== DEBUG: ROOM CONNECTIONS =====");
                foreach (var roomEntry in _rooms)
                {
                    Room room = roomEntry.Value;
                    Console.WriteLine($"Room {room.ID} ({room.Name}) connects to:");
                    List<int> connections = room.GetConnectedRooms();
                    if (connections.Count == 0)
                    {
                        Console.WriteLine("  - NO CONNECTIONS");
                    }
                    else
                    {
                        foreach (int connectedRoomId in connections)
                        {
                            Room connectedRoom = GetRoom(connectedRoomId);
                            string roomName = connectedRoom != null ? connectedRoom.Name : "Unknown Room";
                            Console.WriteLine($"  - Room {connectedRoomId} ({roomName})" +
                                (room.IsConnectionLocked(connectedRoomId) ? " (Locked)" : ""));
                        }
                    }
                }
                Console.WriteLine("=================================");
            }

            public void SetStatistics(Statistics stats)
            {
                _stats = stats;
            }

            public void AddRoom(Room room)
            {
                _rooms[room.ID] = room;
            }

            public Room GetRoom(int roomId)
            {
                return _rooms.ContainsKey(roomId) ? _rooms[roomId] : null;
            }

            public Room GetCurrentRoom()
            {
                return GetRoom(_currentRoomId);
            }

            public List<int> GetAvailableRooms()
            {
                Room currentRoom = GetCurrentRoom();
                if (currentRoom == null)
                {
                    return new List<int>();
                }

                return currentRoom.GetConnectedRooms() ?? new List<int>();
            }

            // Method to prevent automatic combat in special rooms
            public void MoveToAnotherRoom(int roomId, Player player)
            {
                Room currentRoom = GetCurrentRoom();

                if (currentRoom == null || !currentRoom.GetConnectedRooms().Contains(roomId))
                {
                    Console.WriteLine("You cannot go there from here.");
                    return;
                }

                if (currentRoom.IsConnectionLocked(roomId))
                {
                    KeyType requiredKeyType = currentRoom.GetRequiredKeyType(roomId);

                    if (player.HasKeyOfType(requiredKeyType))
                    {
                        Console.WriteLine($"This door is locked and requires a {requiredKeyType} Key.");
                        Console.WriteLine("You have the required key. Use it to unlock the door? (y/n)");

                        if (Console.ReadLine().ToLower().StartsWith("y"))
                        {
                            player.UseKey(requiredKeyType);
                            Console.WriteLine("The door is now unlocked.");
                        }
                        else
                        {
                            Console.WriteLine("You decide not to use the key.");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"This door is locked and requires a {requiredKeyType} Key.");
                        Console.WriteLine("You don't have the required key.");
                        return;
                    }
                }

                _currentRoomId = roomId;
                Room newRoom = GetCurrentRoom();

                if (newRoom != null)
                {
                    if (_stats != null && !newRoom.HasBeenVisited)
                    {
                        _stats.RoomVisited();
                        newRoom.HasBeenVisited = true;
                    }

                    newRoom.GenerateRandomEncounter();

                    Console.Clear();
                    newRoom.DisplayRoom();

                    // Only initiate combat if not a special room with unsolved puzzle.
                    if (newRoom.Monster != null && newRoom.Monster.IsAlive)
                    {
                        if (newRoom.Monster is BossMonster bossMonster && !bossMonster.IsActive)
                        {
                            bossMonster.Activate();
                        }

                        if (!(newRoom is SpecialRoom specialRoom) || specialRoom.PuzzleSolved)
                        {
                            InitiateCombat(player, newRoom.Monster);
                        }
                        else
                        {
                            Console.WriteLine($"The {newRoom.Monster.Name} is in the room but seems distracted by the puzzle mechanism.");
                            Console.WriteLine("You might have time to solve the puzzle before engaging it.");
                        }
                    }

                    PromptForAction(player);
                }
                else
                {
                    Console.WriteLine("Error: The room does not exist.");
                }
            }

            private void InitiateCombat(Player player, Monster monster)
            {
                if (monster == null || !monster.IsAlive) return;

                Console.WriteLine($"\nCombat begins: {player.Name} vs {monster.Name}");
                bool combatEnded = false;
                bool playerEscaped = false;

                while (player.IsAlive && monster.IsAlive && !combatEnded)
                {
                    MonsterBehaviour behavior = monster.DecideBehavior();

                    if (behavior == MonsterBehaviour.Fleeing)
                    {
                        Random random = new Random();
                        int escapeChance = random.Next(100);

                        if (escapeChance < 40)
                        {
                            Console.WriteLine($"{monster.Name} flees from the battle!");
                            combatEnded = true;
                            continue;
                        }
                        else
                        {
                            Console.WriteLine($"{monster.Name} tries to escape but fails!");
                        }
                    }

                    Console.WriteLine($"\n{player.Name}'s turn");
                    DisplayCombatOptions();

                    string input = Console.ReadLine();
                    switch (input.ToLower())
                    {
                        case "1":
                        case "attack":
                            player.Attack(monster);
                            break;

                        case "2":
                        case "potion":
                            HandlePotionUsage(player);
                            break;

                        case "3":
                        case "run":
                            if (TryToEscape())
                            {
                                playerEscaped = true;
                                combatEnded = true;
                            }
                            break;

                        case "4":
                        case "equip":
                            player.EquipStrongestWeapon();
                            Console.WriteLine("You quickly switch weapons!");
                            break;

                        default:
                            Console.WriteLine("Invalid choice. You hesitate and lose your opportunity.");
                            break;
                    }

                    if (combatEnded || !monster.IsAlive) break;

                    if (behavior != MonsterBehaviour.Fleeing)
                    {
                        Console.WriteLine($"\n{monster.Name}'s turn");

                        if (behavior == MonsterBehaviour.Defensive)
                        {
                            Console.WriteLine($"{monster.Name} takes a defensive stance!");
                            monster.Heal(monster.MaxHealth / 10);

                            int originalPower = monster.AttackPower;
                            monster.AttackPower = originalPower / 2;
                            monster.Attack(player);
                            monster.AttackPower = originalPower;
                        }
                        else
                        {
                            monster.Attack(player);
                        }
                    }

                    Console.WriteLine($"\n{player.Name}: {player.CurrentHealth}/{player.MaxHealth} HP | {monster.Name}: {monster.CurrentHealth}/{monster.MaxHealth} HP");
                }

                if (!player.IsAlive)
                {
                    Console.WriteLine($"\n{player.Name} has been defeated by {monster.Name}!");
                    Console.WriteLine("GAME OVER");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
                else if (!monster.IsAlive)
                {
                    Console.WriteLine($"\n{monster.Name} has been defeated!");

                    if (_stats != null)
                    {
                        _stats.MonsterDefeated(monster.ExperienceValue);
                    }

                    player.Experience += monster.ExperienceValue;
                    Console.WriteLine($"You gained {monster.ExperienceValue} experience points.");

                    List<Items> loot = monster.DropLoot();
                    foreach (var item in loot)
                    {
                        Console.WriteLine($"{monster.Name} dropped {item.Name}.");
                        if (player.AddToInventory(item) && _stats != null)
                        {
                            _stats.ItemCollected();
                        }
                    }
                }
                else if (playerEscaped)
                {
                    Console.WriteLine("You managed to escape from the battle!");
                }
                else
                {
                    Console.WriteLine($"{monster.Name} fled from battle. You're safe... for now.");
                }
            }

            private void DisplayCombatOptions()
            {
                Console.WriteLine("What will you do?");
                Console.WriteLine("1. Attack");
                Console.WriteLine("2. Use Potion");
                Console.WriteLine("3. Run Away");
                Console.WriteLine("4. Equip Strongest Weapon");
                Console.Write("Choice: ");
            }

            private void HandlePotionUsage(Player player)
            {
                List<Potions> potions = player.GetAllHealingItems();

                if (potions.Count == 0)
                {
                    Console.WriteLine("You don't have any potions!");
                    return;
                }

                Console.WriteLine("Available Potions:");
                for (int i = 0; i < potions.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {potions[i].Name} (Heals {potions[i].HealAmount} HP)");
                }

                Console.WriteLine("Select a potion to use (0 to cancel):");
                if (int.TryParse(Console.ReadLine(), out int choice))
                {
                    if (choice == 0)
                    {
                        Console.WriteLine("You decide not to use a potion.");
                        return;
                    }

                    if (choice >= 1 && choice <= potions.Count)
                    {
                        Potions selectedPotion = potions[choice - 1];
                        int index = player.Inventory.IndexOf(selectedPotion);

                        if (index >= 0)
                        {
                            player.UsePotion(index);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid selection.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input.");
                }
            }

            private bool TryToEscape()
            {
                Console.WriteLine("You attempt to escape...");

                Random random = new Random();
                int escapeChance = random.Next(100);

                if (escapeChance < 60)
                {
                    Console.WriteLine("You successfully escaped!");
                    return true;
                }
                else
                {
                    Console.WriteLine("You failed to escape!");
                    return false;
                }
            }

            // Updated action prompt that includes the option to engage monsters in special rooms.
            private void PromptForAction(Player player)
            {
                Room currentRoom = GetCurrentRoom();
                bool hasPuzzle = currentRoom is SpecialRoom;
                bool hasMonster = currentRoom.Monster != null && currentRoom.Monster.IsAlive;

                Console.WriteLine("\nWhat would you like to do?");
                Console.WriteLine("1. Look around");
                Console.WriteLine("2. Check inventory");
                Console.WriteLine("3. Collect an item");
                Console.WriteLine("4. Go to another room");
                Console.WriteLine("5. View stats");
                Console.WriteLine("6. Exit game");

                // Only show puzzle option if room has unsolved puzzle
                if (hasPuzzle && !((SpecialRoom)currentRoom).PuzzleSolved)
                {
                    Console.WriteLine("7. Attempt puzzle");
                }

                // Option to engage monster
                if (hasMonster)
                {
                    Console.WriteLine("8. Engage the monster");
                }

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": // Look around
                        GetCurrentRoom().DisplayRoom();
                        PromptForAction(player);
                        break;

                    case "2": // Check inventory
                        Console.Clear();
                        Console.WriteLine("===== INVENTORY =====");
                        player.ShowInventory();

                        Console.WriteLine("\nInventory Options:");
                        Console.WriteLine("1. Use an item");
                        Console.WriteLine("2. Equip strongest weapon");
                        Console.WriteLine("3. Sort by name");
                        Console.WriteLine("4. Filter weapons only");
                        Console.WriteLine("5. Filter potions only");
                        Console.WriteLine("6. Back to room");

                        string inventoryChoice = Console.ReadLine();

                        switch (inventoryChoice)
                        {
                            case "1": // Use an item
                                Console.WriteLine("Enter the number of the item to use (0 to cancel):");
                                if (int.TryParse(Console.ReadLine(), out int itemIndex) && itemIndex > 0 && itemIndex <= player.Inventory.Count)
                                {
                                    player.UseItem(itemIndex - 1);
                                }
                                else if (itemIndex != 0)
                                {
                                    Console.WriteLine("Invalid item number.");
                                }
                                break;

                            case "2": // Equip strongest weapon
                                player.EquipStrongestWeapon();
                                break;

                            case "3": // Sort by name
                                player.ShowInventory(null, (a, b) => string.Compare(a.Name, b.Name));
                                Console.WriteLine("Press any key to continue...");
                                Console.ReadKey();
                                break;

                            case "4": // Filter weapons only
                                player.ShowInventory(item => item is Weapons);
                                Console.WriteLine("Press any key to continue...");
                                Console.ReadKey();
                                break;

                            case "5": // Filter potions only
                                player.ShowInventory(item => item is Potions);
                                Console.WriteLine("Press any key to continue...");
                                Console.ReadKey();
                                break;
                        }

                        PromptForAction(player);
                        break;

                    case "3": // Collect an item
                        Room currentRoomForItems = GetCurrentRoom();
                        currentRoomForItems.DisplayItems();

                        if (currentRoomForItems.Items.Count > 0)
                        {
                            Console.WriteLine("Enter the number of the item to collect (0 to cancel):");
                            if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= currentRoomForItems.Items.Count)
                            {
                                currentRoomForItems.CollectItem(index - 1, player);
                                if (_stats != null) _stats.ItemCollected();
                            }
                            else if (index != 0)
                            {
                                Console.WriteLine("Invalid item number.");
                            }
                        }

                        PromptForAction(player);
                        break;

                    case "4": // Go to another room
                        List<int> availableRooms = GetAvailableRooms();

                        Console.WriteLine("Available rooms:");
                        foreach (int roomId in availableRooms)
                        {
                            Room room = GetRoom(roomId);
                            Console.Write($"{roomId}. {room.Name}");

                            if (GetCurrentRoom().IsConnectionLocked(roomId))
                            {
                                Console.Write(" (Locked)");
                            }

                            Console.WriteLine();
                        }

                        Console.WriteLine("Enter the number of the room to enter (0 to stay here):");
                        if (int.TryParse(Console.ReadLine(), out int roomChoice) && availableRooms.Contains(roomChoice))
                        {
                            MoveToAnotherRoom(roomChoice, player);
                        }
                        else if (roomChoice != 0)
                        {
                            Console.WriteLine("Invalid room selection.");
                            PromptForAction(player);
                        }
                        else
                        {
                            PromptForAction(player);
                        }
                        break;

                    case "5": // View stats
                        Console.Clear();
                        if (_stats != null) _stats.DisplayStatistics();
                        player.DisplayExperience();
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        PromptForAction(player);
                        break;

                    case "6": // Exit game
                        Console.WriteLine("Are you sure you want to exit? (y/n)");
                        if (Console.ReadLine().ToLower().StartsWith("y"))
                        {
                            Console.WriteLine("Thanks for playing! Goodbye!");
                            Environment.Exit(0);
                        }
                        else
                        {
                            PromptForAction(player);
                        }
                        break;

                    case "7": // Attempt puzzle
                        if (hasPuzzle && !((SpecialRoom)currentRoom).PuzzleSolved)
                        {
                            SpecialRoom specialRoom = (SpecialRoom)currentRoom;
                            Console.WriteLine("Enter your puzzle attempt:");
                            string puzzleAttempt = Console.ReadLine();
                            bool puzzleSolved = specialRoom.AttemptPuzzle(player, puzzleAttempt);

                            if (puzzleSolved)
                            {
                                Console.WriteLine("You successfully completed the puzzle!");

                                // If monster present and puzzle solved, start combat
                                if (hasMonster)
                                {
                                    Console.WriteLine($"The {currentRoom.Monster.Name} takes notice of you now that the puzzle is solved!");
                                    Console.WriteLine("Prepare for combat!");
                                    InitiateCombat(player, currentRoom.Monster);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("There is no unsolved puzzle in this room.");
                        }
                        PromptForAction(player);
                        break;

                    case "8": // Engage monster.
                        if (hasMonster)
                        {
                            Console.WriteLine($"You approach the {currentRoom.Monster.Name} ready for battle!");
                            InitiateCombat(player, currentRoom.Monster);
                            PromptForAction(player);
                        }
                        else
                        {
                            Console.WriteLine("There is no monster to engage in this room.");
                            PromptForAction(player);
                        }
                        break;

                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        PromptForAction(player);
                        break;
                }
            }

            public void LoadGameMap()
            {
                // Create regular rooms
                Room entrance = new Room(1, "Entrance Hall", "A dimly lit hall with stone walls and a high ceiling. Torches flicker on the walls.");
                Room caveRoom = new Room(2, "Cave Chamber", "A natural cave with stalactites hanging from the ceiling. Water drips somewhere in the darkness.");

                // Special rooms with puzzles (replacing 3 of the existing rooms)
                RiddleRoom libraryRoom = new RiddleRoom(4, "Ancient Library",
                    "Rows of dusty bookshelves filled with ancient tomes. A strange inscription is etched into the central lectern.");

                MemoryPuzzleRoom treasureRoom = new MemoryPuzzleRoom(3, "Memory Chamber",
                    "A circular room with colored symbols glowing on the walls. In the center stands a pedestal with matching colored crystals.");

                ChessPuzzleRoom throneRoom = new ChessPuzzleRoom(7, "Throne Room",
                    "An impressive chamber with a massive stone throne. A chess-like board is inlaid in the floor with strange pieces positioned on it.");

                // Set rewards for solving puzzles
                libraryRoom.SetReward(new Weapons("Tome of Power", "An ancient book radiating magical energy", 18));
                treasureRoom.SetReward(new Key("Gold Key", "A key made of pure gold", KeyType.Gold));
                throneRoom.SetReward(new Weapons("Royal Scepter", "A powerful royal scepter", 25));

                // Keep the remaining regular rooms
                Room dungeonRoom = new Room(5, "Dungeon Cell", "A grim prison cell with rusted bars and chains hanging from the walls.");
                Room gardenRoom = new Room(6, "Overgrown Garden", "What was once a beautiful garden is now overgrown with strange luminescent plants.");

                // Add rooms to map
                AddRoom(entrance);
                AddRoom(caveRoom);
                AddRoom(treasureRoom); // Puzzle room
                AddRoom(libraryRoom);  // Puzzle room
                AddRoom(dungeonRoom);
                AddRoom(gardenRoom);
                AddRoom(throneRoom);   // Puzzle room

                // Room connections
                entrance.ConnectRoom(2);
                entrance.ConnectRoom(4);

                caveRoom.ConnectRoom(1);
                caveRoom.ConnectRoom(3);
                caveRoom.ConnectLockedRoom(5, KeyType.Bronze);

                treasureRoom.ConnectRoom(2);

                libraryRoom.ConnectRoom(1);
                libraryRoom.ConnectLockedRoom(6, KeyType.Silver);

                dungeonRoom.ConnectRoom(2);

                gardenRoom.ConnectRoom(4);
                gardenRoom.ConnectLockedRoom(7, KeyType.Gold);

                throneRoom.ConnectRoom(6);

                // Add monsters to rooms
                caveRoom.SetMonster(new FrogMonster());
                dungeonRoom.SetMonster(new GnomeMonster());
                gardenRoom.SetMonster(new SeagullMonster());
                throneRoom.SetMonster(new BossMonster());

                // Add items to regular rooms
                entrance.AddItem(new Weapons("Rusty Dagger", "An old but still sharp dagger", 7));
                entrance.AddItem(new Potions("Small Health Potion", "A minor healing elixir", 15));

                caveRoom.AddItem(new Key("Bronze Key", "A key made of bronze, could open a simple lock", KeyType.Bronze));

                dungeonRoom.AddItem(new Weapons("Prisoner's Shiv", "A makeshift but deadly weapon", 9));

                gardenRoom.AddItem(new Potions("Nature's Essence", "A potion made from magical plants", 25));

                libraryRoom.AddItem(new Key("Silver Key", "A shimmering silver key", KeyType.Silver));
            }
        }

        // Design decisions justification (GameMenu class):
        // This has been added so the player has a way of interacting with the game.
        // In addition to this, it keeps the code in one place, making it more organised.

        public class GameMenu
        {
            private Player _player;
            private GameMap _gameMap;
            private Statistics _stats;

            public GameMenu(Player player, GameMap gameMap, Statistics stats = null)
            {
                _player = player;
                _gameMap = gameMap;
                _stats = stats;

                if (_stats != null)
                {
                    _gameMap.SetStatistics(_stats);
                }
            }

            public void ShowMenu()
            {
                bool exitGame = false;

                while (!exitGame)
                {
                    Console.Clear();
                    Console.WriteLine("==================");
                    Console.WriteLine("    GAME MENU     ");
                    Console.WriteLine("==================");
                    Console.WriteLine("1. Start Adventure");
                    Console.WriteLine("2. View Player Stats");
                    Console.WriteLine("3. View Game Statistics");
                    Console.WriteLine("4. Save Game");
                    Console.WriteLine("5. Load Game");
                    Console.WriteLine("6. Exit Game");
                    Console.WriteLine("==================");
                    Console.WriteLine("Choose an option:");

                    string input = Console.ReadLine();

                    switch (input)
                    {
                        case "1":
                            Console.Clear();
                            Room currentRoom = _gameMap.GetCurrentRoom();
                            if (currentRoom != null)
                            {
                                currentRoom.DisplayRoom();

                                Console.WriteLine("\nAvailable Exits:");
                                List<int> availableRooms = _gameMap.GetAvailableRooms();
                                foreach (int roomId in availableRooms)
                                {
                                    Room room = _gameMap.GetRoom(roomId);
                                    if (room != null)
                                    {
                                        Console.WriteLine($"{roomId}. {room.Name}" +
                                            (currentRoom.IsConnectionLocked(roomId) ? " (Locked)" : ""));
                                    }
                                }

                                Console.WriteLine("\nWhich room would you like to enter? (Enter number)");
                                string roomChoice = Console.ReadLine();
                                if (int.TryParse(roomChoice, out int roomNumber) &&
                                    availableRooms.Contains(roomNumber))
                                {
                                    _gameMap.MoveToAnotherRoom(roomNumber, _player);
                                }
                                else
                                {
                                    Console.WriteLine("Invalid room selection.");
                                    WaitForKey();
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error: Could not find current room. Game map may not be initialized properly.");
                                WaitForKey();
                            }
                            break;

                        case "2":
                            Console.Clear();
                            ShowPlayerStatistics();
                            WaitForKey();
                            break;

                        case "3":
                            Console.Clear();
                            if (_stats != null)
                            {
                                _stats.DisplayStatistics();
                            }
                            else
                            {
                                Console.WriteLine("Game statistics are not available.");
                            }
                            WaitForKey();
                            break;

                        case "4":
                            SaveGame();
                            break;

                        case "5":
                            LoadGame();
                            break;

                        case "6":
                            Console.WriteLine("Are you sure you want to exit? (y/n)");
                            if (Console.ReadLine().ToLower().StartsWith("y"))
                            {
                                exitGame = true;
                                Console.WriteLine("Thanks for playing! Goodbye!");
                            }
                            break;

                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            WaitForKey();
                            break;
                    }
                }
            }

            private void ShowPlayerStatistics()
            {
                Console.WriteLine("================================");
                Console.WriteLine("     PLAYER STATISTICS         ");
                Console.WriteLine("================================");
                Console.WriteLine($"Name: {_player.Name}");
                Console.WriteLine($"Health: {_player.CurrentHealth}/{_player.MaxHealth}");
                Console.WriteLine($"Attack Power: {_player.AttackPower}");
                Console.WriteLine($"Defense: {_player.Defense}");
                Console.WriteLine($"Gold: {_player.Gold}");
                _player.DisplayExperience();
                Console.WriteLine("================================");

                if (_player.EquippedWeapon != null)
                {
                    Console.WriteLine($"Equipped Weapon: {_player.EquippedWeapon.Name} (+{_player.EquippedWeapon.Damage} damage)");
                }
                else
                {
                    Console.WriteLine("No weapon equipped");
                }

                Console.WriteLine("\nWould you like to see your inventory? (y/n)");
                if (Console.ReadLine().ToLower().StartsWith("y"))
                {
                    Console.Clear();
                    _player.ShowInventory();
                }
            }

            private void SaveGame()
            {
                try
                {
                    Console.WriteLine("Saving game...");

                    var saveData = new Dictionary<string, string>
                {
                    {"PlayerName", _player.Name},
                    {"PlayerHealth", _player.CurrentHealth.ToString()},
                    {"PlayerMaxHealth", _player.MaxHealth.ToString()},
                    {"PlayerAttack", _player.AttackPower.ToString()},
                    {"PlayerDefense", _player.Defense.ToString()},
                    {"PlayerExp", _player.Experience.ToString()},
                    {"CurrentRoom", _gameMap.GetCurrentRoom().ID.ToString()}
                };

                    using (StreamWriter writer = new StreamWriter("savegame.txt"))
                    {
                        foreach (var item in saveData)
                        {
                            writer.WriteLine($"{item.Key}={item.Value}");
                        }
                    }

                    Console.WriteLine("Game saved successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save game: {ex.Message}");
                }

                WaitForKey();
            }

            private void LoadGame()
            {
                try
                {
                    Console.WriteLine("Loading game...");

                    if (!File.Exists("savegame.txt"))
                    {
                        Console.WriteLine("No saved game found.");
                        WaitForKey();
                        return;
                    }

                    var saveData = new Dictionary<string, string>();

                    using (StreamReader reader = new StreamReader("savegame.txt"))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] parts = line.Split('=');
                            if (parts.Length == 2)
                            {
                                saveData[parts[0]] = parts[1];
                            }
                        }
                    }

                    if (saveData.ContainsKey("PlayerName"))
                    {
                        Console.WriteLine($"Loaded save data for player: {saveData["PlayerName"]}");

                        if (saveData.ContainsKey("CurrentRoom"))
                        {
                            int roomId = int.Parse(saveData["CurrentRoom"]);
                            Console.WriteLine($"Restoring position to room {roomId}");
                        }
                    }

                    Console.WriteLine("Game loaded successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load game: {ex.Message}");
                }

                WaitForKey();
            }

            private void WaitForKey()
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }

    
    namespace DungeonExplorer.Tests
    {
        using Microsoft.VisualStudio.TestTools.UnitTesting;
        using System;
        using System.Collections.Generic;
        using System.Diagnostics;
        using System.IO;
        using System.Text;

        [TestClass]
        public class GameTests
        {
            private static StreamWriter _logWriter;
            private static string _logPath;

            [ClassInitialize]
            public static void ClassInitialization(TestContext context)
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _logPath = $"GameTest_Results_{timestamp}.log";
                _logWriter = new StreamWriter(_logPath, true);
                LogMessage("Game system tests have begun.");
            }

            [ClassCleanup]
            public static void ClassCleanup()
            {
                LogMessage("Game system tests are complete.");
                _logWriter.Flush();
                _logWriter.Close();

                Console.WriteLine($"Test log file created at: {Path.GetFullPath(_logPath)}");
            }

            [TestInitialize]
            public void TestInitialization()
            {
                LogMessage($"Starting test: {TestContext.TestName}");
            }

            [TestCleanup]
            public void TestCleanUp()
            {
                LogMessage($"Completed test: {TestContext.TestName}");
            }

            private static void LogMessage(string message)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string formattedMessage = $"[{timestamp}] {message}";
                _logWriter.WriteLine(formattedMessage);
                _logWriter.Flush();

                // Also output to console for real-time monitoring
                Console.WriteLine(formattedMessage);
            }

            // Test player creation and basic stats
            [TestMethod]
            public void Player_Creation_Test()
            {
                try
                {
                    // Arrange
                    LogMessage("Creating test player");
                    string name = "TestHero";
                    int health = 100;
                    int attack = 15;
                    int defense = 8;

                    // Act
                    var player = new Program.Player(name, health, attack, defense);

                    // Assert with Debug.Assert for code verification
                    Debug.Assert(player.Name == name, "Debug: Player name should match input");
                    Debug.Assert(player.MaxHealth == health, "Debug: Player max health should match input");
                    Debug.Assert(player.AttackPower == attack, "Debug: Player attack should match input");
                    Debug.Assert(player.Defense == defense, "Debug: Player defense should match input");
                    Debug.Assert(player.IsAlive, "Debug: Player should be alive when created");

                    Assert.AreEqual(name, player.Name, "Player name should match input");
                    Assert.AreEqual(health, player.MaxHealth, "Player max health should match input");
                    Assert.AreEqual(health, player.CurrentHealth, "Player current health should equal max health");
                    Assert.AreEqual(attack, player.AttackPower, "Player attack should match input");
                    Assert.AreEqual(defense, player.Defense, "Player defense should match input");
                    Assert.IsTrue(player.IsAlive, "Player should be alive when created");

                    LogMessage("Player_Creation_Test: PASSED");
                }
                catch (Exception ex)
                {
                    LogMessage($"Player_Creation_Test: FAILED. Exception: {ex.Message}");
                    LogMessage($"Stack Trace: {ex.StackTrace}");
                    throw;
                }
            }

            // Test player inventory management
            [TestMethod]
            public void Player_Inventory_Management_Test()
            {
                try
                {
                    // Arrange
                    LogMessage("Testing inventory management");
                    var player = new Program.Player("InventoryTester", 100, 10, 5);
                    var sword = new Program.Weapons("Test Sword", "A test weapon", 10);
                    var potion = new Program.Potions("Test Potion", "A test healing item", 20);

                    // Act & Assert: Add items to inventory
                    player.AddToInventory(sword);
                    player.AddToInventory(potion);

                    Debug.Assert(player.Inventory.Count == 2, "Debug: Inventory should contain two items");
                    Assert.AreEqual(2, player.Inventory.Count, "Inventory should contain two items");

                    // Act & Assert: Equip weapon
                    player.EquipWeapon(sword);

                    Debug.Assert(player.EquippedWeapon == sword, "Debug: Player should have the sword equipped");
                    Assert.AreEqual(sword, player.EquippedWeapon, "Player should have the sword equipped");

                    // Create console output capture
                    var consoleOutput = new StringWriter();
                    var originalOutput = Console.Out;
                    Console.SetOut(consoleOutput);

                    // Act & Assert: Use potion
                    int initialHealth = player.CurrentHealth;
                    player.CurrentHealth = 50; // Set health lower to test potion
                    int potionIndex = player.Inventory.IndexOf(potion);
                    player.UsePotion(potionIndex);

                    Console.SetOut(originalOutput);

                    Debug.Assert(player.Inventory.Count == 1, "Debug: Inventory should have one item after using potion");
                    Assert.AreEqual(1, player.Inventory.Count, "Inventory should have one item after using potion");

                    string output = consoleOutput.ToString();
                    Debug.Assert(output.Contains("heals for"), "Debug: Output should indicate healing occurred");
                    Assert.IsTrue(output.Contains("heals for"), "Output should indicate healing occurred");

                    LogMessage("Player_Inventory_Management_Test: PASSED");
                }
                catch (Exception ex)
                {
                    LogMessage($"Player_Inventory_Management_Test: FAILED. Exception: {ex.Message}");
                    LogMessage($"Stack Trace: {ex.StackTrace}");
                    throw;
                }
            }

            // Test combat system
            [TestMethod]
            public void Combat_System_Test()
            {
                try
                {
                    // Arrange
                    LogMessage("Testing combat system");
                    var player = new Program.Player("CombatTester", 100, 20, 5);
                    var monster = new Program.FrogMonster();
                    int initialMonsterHealth = monster.CurrentHealth;

                    // Create console output capture
                    var consoleOutput = new StringWriter();
                    var originalOutput = Console.Out;
                    Console.SetOut(consoleOutput);

                    // Act: Player attacks monster
                    player.Attack(monster);

                    // Assert: Monster health should decrease
                    Console.SetOut(originalOutput);
                    string output = consoleOutput.ToString();

                    Debug.Assert(monster.CurrentHealth < initialMonsterHealth, "Debug: Monster health should decrease after attack");
                    Assert.IsTrue(monster.CurrentHealth < initialMonsterHealth, "Monster health should decrease after attack");

                    Debug.Assert(output.Contains("attacks"), "Debug: Output should describe the attack");
                    Assert.IsTrue(output.Contains("attacks"), "Output should describe the attack");

                    // Reset console output capture for monster attack
                    consoleOutput = new StringWriter();
                    Console.SetOut(consoleOutput);

                    // Act: Monster attacks player
                    int initialPlayerHealth = player.CurrentHealth;
                    monster.Attack(player);

                    // Assert: Player health should decrease
                    Console.SetOut(originalOutput);
                    output = consoleOutput.ToString();

                    Debug.Assert(player.CurrentHealth < initialPlayerHealth, "Debug: Player health should decrease after monster attack");
                    Assert.IsTrue(player.CurrentHealth < initialPlayerHealth, "Player health should decrease after monster attack");

                    Debug.Assert(output.Contains("attacks"), "Debug: Output should describe the monster attack");
                    Assert.IsTrue(output.Contains("attacks"), "Output should describe the monster attack");

                    LogMessage("Combat_System_Test: PASSED");
                }
                catch (Exception ex)
                {
                    LogMessage($"Combat_System_Test: FAILED. Exception: {ex.Message}");
                    LogMessage($"Stack Trace: {ex.StackTrace}");
                    throw;
                }
            }

            // Test room navigation and interconnection
            [TestMethod]
            public void Room_Navigation_Test()
            {
                try
                {
                    // Arrange
                    LogMessage("Testing room navigation");
                    var gameMap = new Program.GameMap();

                    // Create test rooms
                    Program.Room room1 = new Program.Room(1, "Test Room 1", "A test room");
                    Program.Room room2 = new Program.Room(2, "Test Room 2", "Another test room");
                    Program.Room room3 = new Program.Room(3, "Test Room 3", "A third test room");

                    // Connect rooms
                    room1.ConnectRoom(2);
                    room2.ConnectRoom(1);
                    room2.ConnectRoom(3);
                    room3.ConnectRoom(2);

                    // Add rooms to map
                    gameMap.AddRoom(room1);
                    gameMap.AddRoom(room2);
                    gameMap.AddRoom(room3);

                    // Act & Assert: Check connections
                    var room1Connections = room1.GetConnectedRooms();
                    var room2Connections = room2.GetConnectedRooms();
                    var room3Connections = room3.GetConnectedRooms();

                    Debug.Assert(room1Connections.Contains(2), "Debug: Room 1 should connect to Room 2");
                    Debug.Assert(room2Connections.Contains(1), "Debug: Room 2 should connect to Room 1");
                    Debug.Assert(room2Connections.Contains(3), "Debug: Room 2 should connect to Room 3");
                    Debug.Assert(room3Connections.Contains(2), "Debug: Room 3 should connect to Room 2");

                    Assert.IsTrue(room1Connections.Contains(2), "Room 1 should connect to Room 2");
                    Assert.IsTrue(room2Connections.Contains(1), "Room 2 should connect to Room 1");
                    Assert.IsTrue(room2Connections.Contains(3), "Room 2 should connect to Room 3");
                    Assert.IsTrue(room3Connections.Contains(2), "Room 3 should connect to Room 2");

                    // Test locked room
                    room1.ConnectLockedRoom(3, Program.KeyType.Silver);
                    Assert.IsTrue(room1.IsConnectionLocked(3), "Connection from Room 1 to Room 3 should be locked");
                    Assert.AreEqual(Program.KeyType.Silver, room1.GetRequiredKeyType(3), "Room 1 to Room 3 should require a Silver key");

                    LogMessage("Room_Navigation_Test: PASSED");
                }
                catch (Exception ex)
                {
                    LogMessage($"Room_Navigation_Test: FAILED. Exception: {ex.Message}");
                    LogMessage($"Stack Trace: {ex.StackTrace}");
                    throw;
                }
            }

            // Test monster behavior AI
            [TestMethod]
            public void Monster_Behavior_Test()
            {
                try
                {
                    // Arrange
                    LogMessage("Testing monster behavior AI");
                    var monster = new Program.FrogMonster();

                    // Test aggressive behavior (default)
                    Program.MonsterBehaviour behaviour = monster.Behaviour;
                    Assert.AreEqual(Program.MonsterBehaviour.Aggressive, behaviour, "Monster should start with aggressive behavior");

                    // Simulate damage to trigger defensive behavior
                    int damage = (int)(monster.MaxHealth * 0.6); // Reduce to 40% health
                    monster.DamageTaken(damage);

                    behaviour = monster.DecideBehavior();
                    Assert.AreEqual(Program.MonsterBehaviour.Defensive, behaviour, "Monster should become defensive at low health");

                    // Simulate more damage to trigger fleeing behavior
                    monster.DamageTaken(monster.MaxHealth / 4); // Reduce to less than 20% health

                    behaviour = monster.DecideBehavior();
                    Assert.AreEqual(Program.MonsterBehaviour.Fleeing, behaviour, "Monster should try to flee at very low health");

                    // Test boss monster behavior
                    var bossMonster = new Program.BossMonster();
                    behaviour = bossMonster.DecideBehavior();
                    Assert.AreEqual(Program.MonsterBehaviour.Aggressive, behaviour, "Boss should always be aggressive");

                    // Boss activation test
                    Assert.IsFalse(bossMonster.IsActive, "Boss should start inactive");
                    bossMonster.Activate();
                    Assert.IsTrue(bossMonster.IsActive, "Boss should be active after activation");

                    LogMessage("Monster_Behavior_Test: PASSED");
                }
                catch (Exception ex)
                {
                    LogMessage($"Monster_Behavior_Test: FAILED. Exception: {ex.Message}");
                    LogMessage($"Stack Trace: {ex.StackTrace}");
                    throw;
                }
            }

            // Test LINQ functionality for inventory
            [TestMethod]
            public void LINQ_Inventory_Test()
            {
                try
                {
                    // Arrange
                    LogMessage("Testing LINQ functionality for inventory");
                    var player = new Program.Player("LINQTester", 100, 10, 5);

                    // Add varied items
                    player.AddToInventory(new Program.Weapons("Weak Sword", "A weak sword", 5));
                    player.AddToInventory(new Program.Potions("Small Potion", "A minor healing potion", 10));
                    player.AddToInventory(new Program.Weapons("Strong Sword", "A powerful sword", 15));
                    player.AddToInventory(new Program.Potions("Large Potion", "A major healing potion", 25));
                    player.AddToInventory(new Program.Key("Bronze Key", "A simple key", Program.KeyType.Bronze));

                    // Act & Assert

                    // Test getting all healing items
                    var healingItems = player.GetAllHealingItems();

                    Debug.Assert(healingItems.Count == 2, "Debug: Should find 2 healing items");
                    Debug.Assert(healingItems[0].HealAmount > healingItems[1].HealAmount, "Debug: Healing items should be sorted by heal amount");

                    Assert.AreEqual(2, healingItems.Count, "Should find 2 healing items");
                    Assert.IsTrue(healingItems[0].HealAmount > healingItems[1].HealAmount, "Healing items should be sorted by heal amount");

                    // Test key type check
                    bool hasBronzeKey = player.HasKeyOfType(Program.KeyType.Bronze);
                    bool hasSilverKey = player.HasKeyOfType(Program.KeyType.Silver);

                    Assert.IsTrue(hasBronzeKey, "Player should have a bronze key");
                    Assert.IsFalse(hasSilverKey, "Player should not have a silver key");

                    // Test equip strongest weapon
                    player.EquipStrongestWeapon();

                    Debug.Assert(player.EquippedWeapon.Name == "Strong Sword", "Debug: Strongest weapon should be equipped");
                    Debug.Assert(player.EquippedWeapon.Damage == 15, "Debug: Strongest weapon should have 15 damage");

                    Assert.AreEqual("Strong Sword", player.EquippedWeapon.Name, "Strongest weapon should be equipped");
                    Assert.AreEqual(15, player.EquippedWeapon.Damage, "Strongest weapon should have 15 damage");

                    LogMessage("LINQ_Inventory_Test: PASSED");
                }
                catch (Exception ex)
                {
                    LogMessage($"LINQ_Inventory_Test: FAILED. Exception: {ex.Message}");
                    LogMessage($"Stack Trace: {ex.StackTrace}");
                    throw;
                }
            }

            // Test Statistics tracking
            [TestMethod]
            public void Statistics_Tracking_Test()
            {
                try
                {
                    // Arrange
                    LogMessage("Testing statistics tracking");
                    var stats = new Program.Statistics();
                    var player = new Program.Player("StatsTester", 100, 10, 5);

                    stats.TrackPlayer(player);

                    // Act
                    stats.MonsterDefeated(10); // Defeat a monster worth 10 XP
                    stats.RoomVisited(); // Visit one room
                    stats.ItemCollected(); // Collect one item
                    stats.ItemCollected(); // Collect another item

                    // Create console output capture
                    var consoleOutput = new StringWriter();
                    var originalOutput = Console.Out;
                    Console.SetOut(consoleOutput);

                    // Display stats
                    stats.DisplayStatistics();

                    // Assert
                    Console.SetOut(originalOutput);
                    string output = consoleOutput.ToString();

                    Debug.Assert(output.Contains("Monsters Defeated: 1"), "Debug: Should show 1 monster defeated");
                    Debug.Assert(output.Contains("Rooms Visited: 1"), "Debug: Should show 1 room visited");
                    Debug.Assert(output.Contains("Items Collected: 2"), "Debug: Should show 2 items collected");

                    Assert.IsTrue(output.Contains("Monsters Defeated: 1"), "Should show 1 monster defeated");
                    Assert.IsTrue(output.Contains("Rooms Visited: 1"), "Should show 1 room visited");
                    Assert.IsTrue(output.Contains("Items Collected: 2"), "Should show 2 items collected");

                    LogMessage("Statistics_Tracking_Test: PASSED");
                }
                catch (Exception ex)
                {
                    LogMessage($"Statistics_Tracking_Test: FAILED. Exception: {ex.Message}");
                    LogMessage($"Stack Trace: {ex.StackTrace}");
                    throw;
                }
            }

            // Test Special Room puzzle functionality
            [TestMethod]
            public void SpecialRoom_Puzzle_Test()
            {
                try
                {
                    // Arrange
                    LogMessage("Testing special room puzzle functionality");
                    var player = new Program.Player("PuzzleTester", 100, 10, 5);

                    // Test Riddle Room
                    var riddleRoom = new Program.RiddleRoom(1, "Test Riddle Room", "A room with a riddle");
                    var rewardWeapon = new Program.Weapons("Puzzle Sword", "A reward for solving the riddle", 15);
                    riddleRoom.SetReward(rewardWeapon);

                    // Access the private fields through reflection to set a known riddle/answer for testing
                    var riddleType = typeof(Program.RiddleRoom);
                    var riddleField = riddleType.GetField("_currentRiddle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var answerField = riddleType.GetField("_currentAnswer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (riddleField != null && answerField != null)
                    {
                        riddleField.SetValue(riddleRoom, "Test riddle");
                        answerField.SetValue(riddleRoom, "test");
                    }

                    // Create console output capture
                    var consoleOutput = new StringWriter();
                    var originalOutput = Console.Out;
                    Console.SetOut(consoleOutput);

                    // Act & Assert: Attempt puzzle with correct answer
                    bool result = riddleRoom.AttemptPuzzle(player, "test");

                    Console.SetOut(originalOutput);
                    string output = consoleOutput.ToString();

                    Assert.IsTrue(result, "Puzzle should be solved with correct answer");
                    Assert.IsTrue(output.Contains("Congratulations"), "Output should congratulate player");
                    Assert.IsTrue(player.Inventory.Contains(rewardWeapon), "Player should receive reward");

                    LogMessage("SpecialRoom_Puzzle_Test: PASSED");
                }
                catch (Exception ex)
                {
                    LogMessage($"SpecialRoom_Puzzle_Test: FAILED. Exception: {ex.Message}");
                    LogMessage($"Stack Trace: {ex.StackTrace}");
                    throw;
                }
            }

            public TestContext TestContext { get; set; }
        }
    }

}