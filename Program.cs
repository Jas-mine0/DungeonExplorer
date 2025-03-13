using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dungeon_Explorer.Program;

namespace Dungeon_Explorer
{
    public class Program
    {

        public class Game
        {
            public static void Main(string[] args)
            {
                {
                    Player player = new Player();
                    Console.WriteLine("The player's name is: " + player.playerName);
                    // The code below creates a "Monster" object.
                    Monster room1Monster = new Monster();
                    // The code below is to create a room.
                    Room room = new Room(room1Monster);
                    // The code below this has been commented out because it gave the user
                    // the same description as the first room.
                    // Monster_2 Room_2_Monster = new Monster_2();

                    // The code below is to add an additional room.
                    // Room_2 room_2 = new Room_2(Room_2_Monster);
                }
            }
        }

        public class Player
        {
            public string playerName;

            public Player()
            {
                while (true)
                {
                    Console.WriteLine("Please enter your name: ");
                    playerName = Console.ReadLine();
                    if (string.IsNullOrEmpty(playerName))
                    {
                        Console.WriteLine("You can't have an empty name. Please enter a name.");
                    }
                    else
                    {
                        Console.WriteLine("Hello, " + playerName);
                        break;
                    }
                    int Player_Health;
                    Player_Health = 50;
                    Console.WriteLine("Your Health is: " + Player_Health);
                    Console.WriteLine("Pick a room to go into: (1/2)");
                    Console.ReadLine();
                }
            }

            public class Inventory<T>
            {
                private List<T> items;

                public Inventory()
                {
                    items = new List<T>();

                    Console.WriteLine("Inventory stuff: ");
                    foreach (var item in items)
                    {
                        Console.WriteLine(item);
                    }
                }
            }
        }


        public class Room
        {
            public string roomDescription = "You have entered a bleak, dark looking room.";
            public string Item { get; set; } = "Apple.";
            public Monster room1Monster { get; set; }

            public Room(Monster room1Monster = null)
            {
                Console.WriteLine("Room description: " + roomDescription);
                Console.WriteLine("You have picked up an item: Apple.");
                room1Monster = room1Monster;
                if (room1Monster != null)
                {
                    Console.WriteLine("You have encountered a monster: " + room1Monster.monsterDescription);
                }
            }
        }

        public class Monster
        {
            public string monsterDescription { get; set; } = "You have encountered an oversized frog.";

            public Monster()
            {
                Console.WriteLine("Monster Description: " + monsterDescription);
            }
        }

        public class Room2
        {
            public string room2Description { get; set; } = "You have entered a room with bright lights.";

            public Room2()
            {
                Console.WriteLine("Room_Description: " + room2Description);
            }
        }


        // The code below will be implemented at a later date.
        // public class monster2
        // {
            // public string monster2Description { get; set; } = "You have encountered a giant marshmellow monster.";

            // public monster2(monster room2Monster = null);
                // if (room2Monster != null)
                // {
                    // Console.WriteLine("You have encountered a monster: " + room2Monster.monsterDescription);
                // }
        }
    }
