using System;
using System.Collections.Generic;
using static Genealogy.Utility;
using System.Linq;
using System.Text;
using System.Threading;

namespace Genealogy
{
    internal class FamilyTree
    {
        public static SqlDatabase Database { get; set; } = new SqlDatabase();

        /// <summary>
        /// Sets the program up with "mock data" and runs the main menu.
        /// </summary>
        public void Start()
        {
            PrintTitle("Title");
            SetUp();
            MainMenu();
        }

        /// <summary>
        /// The user will be able to choose to add a new member or search for members.
        /// The method will loop until the user makes the choice to exit the program.
        /// </summary>
        public static void MainMenu()
        {
            while (true)
            {
                PrintTitle("Main Menu");
                WriteInColor("\tWhat do you want to do?\n");
                Console.WriteLine("\n\t1. Add a family member");
                Console.WriteLine("\t2. Search for family members");
                WriteInColor("\tE. Exit the program\n", ConsoleColor.DarkRed);
                bool innerExit;
                do
                {
                    innerExit = true;
                    Console.Write("\t> ");
                    var choice = ReadLine();
                    switch (choice.ToUpper())
                    {
                        case "1":
                            PrintTitle("Add Member");
                            WriteInColor("\tEnter 0 to go back to the main menu\n\n");
                            AddMember();
                            break;
                        case "2":
                            SearchMembers();
                            break;
                        case "E":
                            WriteDelayed("\n\tExiting the program");
                            WriteDelayed("...", delay: 600);
                            Thread.Sleep(800);
                            Environment.Exit(0);
                            break;
                        default:
                            ErrorMessage("\tInvalid choice. try again!");
                            innerExit = false;
                            break;
                    }                    
                } while (!innerExit);
            }
        }

        /// <summary>
        /// Adds a member to the Family Tree.
        /// </summary>
        /// <returns>The member id.</returns>
        public static int AddMember(bool standard = true)
        {            
            var firstName = GetName("first");
            if (firstName == "0")
            {
                return 0;
            }

            var lastName = GetName("last");
            if (lastName == "0")
            {
                return 0;
            }

            var member = new Member
            {
                FirstName = char.ToUpper(firstName[0]) + firstName[1..],
                LastName = char.ToUpper(lastName[0]) + lastName[1..]
            };

            if (Database.DoesMemberExist(member.ToString()))
            {
                var choice = DoesAlreadyExist(member.ToString());
                if (choice.ToLower() != "y")
                { 
                    return 0; 
                }
                Console.WriteLine();
            }

            member.DateOfBirth = GetDateTime("birth");

            var placeOfBirthId = Database.GetPlaceId("birth");
            if (placeOfBirthId != 0)
            {
                member.PlaceOfBirthId = placeOfBirthId;
            }

            Database.CreateBasicMember(member);
            member.Id = Database.GetLastAddedId("Family");

            if (standard)
            {
                if (MakeAChoice("\n\tDo you want to add more details(y/n)?", ConsoleColor.Yellow))
                {
                    AddDetails(member);
                }
            }
            Success($"{member.FirstName} {member.LastName}");
            return member.Id;
        }

        private static void AddDetails(Member member)
        {
            if (MakeAChoice("\tIs the member deceased(y/n)?"))
            {
                member.DateOfDeath = GetDateTime("death");

                var placeOfDeathId = Database.GetPlaceId("death");
                if (placeOfDeathId != 0)
                {
                    member.PlaceOfDeathId = placeOfDeathId;
                }
                Console.WriteLine();
            }

            if (MakeAChoice("\tDo you want to set partner(y/n)? "))
            {
                member.PartnerId = Database.SetMember("partner");
            }

            if (MakeAChoice("\tDo you want to set parents(y/n)?"))
            {
                SetParents(member);
            }
            Console.WriteLine();
            Database.UpdateMember(member);
        }

        private static void SetParents(Member member)
        {
            var fatherId = Database.SetMember("father");
            if (fatherId != 0)
            {
                member.FatherId = fatherId;
                Console.WriteLine();
            }
            
            var motherId = Database.SetMember("mother");
            if (motherId != 0)
            {
                member.MotherId = motherId;
            }
        }

        /// <summary>
        /// Prompts the user to choose a condition for the search. The user
        /// will then be shown all the members that match the search and be
        /// able to select one of the matching members. The method will loop
        /// until the user chooses to exit back to the main menu.
        /// </summary>
        private static void SearchMembers()
        {
            while (true)
            {
                PrintTitle("List Members");
                WriteInColor("\tChoose the conditions for your search\n");
                Console.WriteLine("\n\tA. All the members in the family tree");
                Console.WriteLine("\t1. Members of a given name");
                Console.WriteLine("\t2. Members within an age range");
                Console.WriteLine("\t3. Members born a certain date");
                Console.WriteLine("\t4. Members who lack certain data");
                Console.WriteLine("\t5. Members that are alive");
                Console.WriteLine("\t6. Members that are deceased");
                WriteInColor("\tE. Exit to main menu\n", ConsoleColor.DarkRed);
                var members = new List<Member>();
                bool innerExit;
                do
                {
                    innerExit = true;
                    var choice = GetInput("\t> ");
                    if (choice.ToUpper() == "E")
                    {
                        MainMenu();
                    }
                    else if (choice.ToUpper() == "A")
                    {
                        members = Database.Search();
                    }
                    else
                    {
                        TryParse(choice, out int option);
                        Console.WriteLine();
                        var allMembers = Database.Search();
                        switch (option)
                        {
                            case 1:
                                var name = GetInput("\tEnter a name: ", ConsoleColor.Yellow);
                                members = Database.SearchByName(name);
                                break;
                            case 2:
                                var minAge = GetAge("min");
                                var maxAge = GetAge("max");
                                var today = DateTime.Today;
                                foreach (var memb in allMembers)
                                {
                                    if (memb.DateOfBirth != null)
                                    {
                                        var age = today.Year - memb.DateOfBirth.Value.Year;
                                        if (memb.DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                                        if (age >= minAge && age <= maxAge) members.Add(memb);
                                    }
                                }
                                break;
                            case 3:
                                var year = GetInput("\tEnter a date: ", ConsoleColor.Yellow);
                                members = Database.SearchByDate(year);
                                break;
                            case 4:
                                members = MissingData();
                                break;
                            case 5:
                                members = allMembers.Where(m => m.DateOfDeath == null && m.PlaceOfDeathId == null).ToList();
                                break;
                            case 6:
                                members = allMembers.Where(m => m.DateOfDeath != null && m.PlaceOfDeathId != null).ToList();
                                break;
                            default:
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                ErrorMessage("\tInvalid choice. Try again!");
                                innerExit = false;
                                break;
                        }
                    }
                } while (!innerExit);

                if (members.Count > 0)
                {
                    var id = Database.ChooseMember(members);
                    if (id != 0)
                    {
                        var member = members.Where(m => m.Id == id).FirstOrDefault();
                        SelectedMember(member);
                    }
                }
                else ErrorMessage("\tNo members matched your search.");
            }
        }

        private static List<Member> MissingData()
        {
            WriteInColor("\tWhat data should be missing?\n");
            Console.WriteLine("\t1. Date of birth");
            Console.WriteLine("\t2. Place of birth");
            Console.WriteLine("\t3. Date of death");
            Console.WriteLine("\t4. Place of death");
            Console.WriteLine("\t5. Father");
            Console.WriteLine("\t6. Mother");
            WriteInColor("\t0. Go back\n");
            WriteInColor("\tE. Exit to main menu\n", ConsoleColor.DarkRed);
            var members = new List<Member>();
            while (true)
            {
                var choice = GetInput("\t> ");
                if (choice.ToUpper() == "E")
                {
                    MainMenu();
                }
                else if (choice == "0") 
                { 
                    return null; 
                }
                else
                {
                    TryParse(choice, out int option);
                    var sql = "WHERE ";
                    switch (option)
                    {
                        case 1:
                            sql += "date_of_birth";
                            break;
                        case 2:
                            sql += "place_of_birth_id";
                            break;
                        case 3:
                            sql += "date_of_death";
                            break;
                        case 4:
                            sql += "place_of_death_id";
                            break;
                        case 5:
                            sql += "father_id";
                            break;
                        case 6:
                            sql += "mother_id";
                            break;
                        default:
                            ErrorMessage("\tInvalid choice. Try Again!");
                            break;
                    }
                    sql += " IS NULL";
                    members = Database.Search(sql);
                    break;
                }
            }
            return members;
        }

        /// <summary>
        /// The user will be able to show relatives of the selected member,
        /// update the member or delete the member. The method will loop until
        /// the user chooses to go back or exit to main menu.
        /// </summary>
        /// <param name="member"></param>
        /// <returns><see langword="true"/> if the user chooses to exit to
        /// main menu, otherwise <see langword="false"/>.</returns>
        private static void SelectedMember(Member member)
        {
            var outerExit = false;
            while (!outerExit)
            {
                PrintTitle("Selected Member");
                WriteInColor($"\tWhat do you want to do with {member}?\n");
                DisplayDetails(member);
                Console.WriteLine("\n\t1. Show relatives");
                Console.WriteLine("\t2. Update member");
                Console.WriteLine("\t3. Delete member");
                WriteInColor("\t0. Go back\n");
                WriteInColor("\tE. Exit to main menu\n", ConsoleColor.DarkRed);
                bool innerExit;
                do
                {
                    innerExit = true;
                    Console.Write("\t> ");
                    var choice = ReadLine();
                    if (choice.ToUpper() == "E")
                    {
                        MainMenu();
                    }
                    else if (choice == "0")
                    {
                        outerExit = true;
                    }
                    else if (choice == "1")
                    {
                        ShowRelatives(member);
                    }
                    else if (choice == "2")
                    {
                        UpdateMember(member);
                    }
                    else if (choice == "3")
                    {
                        if (DeleteMember(member))
                        {
                            outerExit = true;
                        }
                    }
                    else
                    {
                        ErrorMessage("Invalid choice. Try again!");
                        innerExit = false;
                    }
                } while (!innerExit);
            }
        }

        /// <summary>
        /// Lets the user choose what kind of relatives to show. The user
        /// will then be shown all the members of that category if any and
        /// be able to select a relative. The method will loop until the user
        /// chooses to go back or to exit back to main menu.
        /// </summary>
        /// <param name="member"></param>
        /// <returns><see langword="true"/> if the user chooses to exit
        /// to main menu, otherwise <see langword="false"/>.</returns>
        private static void ShowRelatives(Member member)
        {
            var outerExit = false;
            while (!outerExit)
            {
                PrintTitle("Relatives");
                WriteInColor($"\tWhich of {member}'s relatives do you want to show?\n");
                Console.WriteLine("\n\t1. Parents");
                Console.WriteLine("\t2. Children");
                Console.WriteLine("\t3. Partner");
                Console.WriteLine("\t4. Siblings");
                Console.WriteLine("\t5. Cousins");
                Console.WriteLine("\t6. Aunts and uncles");
                Console.WriteLine("\t7. Grandparents");
                WriteInColor("\t0. Go back\n");
                WriteInColor("\tE. Exit to main menu\n", ConsoleColor.DarkRed);
                bool innerExit;
                do
                {
                    innerExit = true;
                    Console.Write("\t> ");
                    var choice = ReadLine();
                    if (choice.ToUpper() == "E")
                    {
                        MainMenu();
                    }
                    else if (choice == "0")
                    {
                        outerExit = true;
                    }
                    else if (TryParse(choice, out int option))
                    {
                        if (option <= 7)
                        {
                            var relatives = Database.GetRelatives(member, option, out string type);
                            if (relatives.Count > 0)
                            {
                                var id = Database.ChooseMember(relatives);
                                if (id != 0)
                                {
                                    var relative = Database.SearchById(id);
                                    SelectedMember(relative);
                                }
                            }
                            else ErrorMessage($"\t{member} doesn't have any {type}.");
                        }
                    }
                    else
                    {
                        ErrorMessage("\tInvalid choice. Try again!");
                        innerExit = false;
                    }
                } while (!innerExit);
            }
        }

        /// <summary>
        /// Lets the user choose what part of the member to update. The method
        /// will loop until the user chooses to go back or exit to main menu.
        /// </summary>
        /// <param name="member"></param>
        /// <returns><see langword="true"/> if the user chooses to exit to
        /// main menu, otherwise <see langword="false"/>.</returns>
        private static void UpdateMember(Member member)
        {
            var outerExit = false;
            while (!outerExit)
            {
                PrintTitle("Update Member");
                WriteInColor($"\tWhat about {member} do you want to update?\n");
                DisplayDetails(member);
                Console.WriteLine("\n\t1. First name");
                Console.WriteLine("\t2. Last name");
                Console.WriteLine("\t3. Date of birth");
                Console.WriteLine("\t4. Place of birth");
                Console.WriteLine("\t5. Date of death");
                Console.WriteLine("\t6. Place of death");
                Console.WriteLine("\t7. Partner");
                Console.WriteLine("\t8. Father");
                Console.WriteLine("\t9. Mother");
                WriteInColor("\t0. Go back\n");
                WriteInColor("\tE. Exit to main menu\n", ConsoleColor.DarkRed);
                bool innerExit;
                do
                {
                    innerExit = true;
                    Console.Write("\t> ");
                    var choice = ReadLine();
                    if (choice.ToUpper() == "E")
                    {
                        MainMenu();
                    }
                    else if (choice == "0")
                    {
                        outerExit = true;
                    }
                    else
                    {
                        TryParse(choice, out int option);
                        Console.WriteLine();
                        switch (option)
                        {
                            case 1:
                                member.FirstName = GetName("first");
                                break;
                            case 2:
                                member.LastName = GetName("last");
                                break;
                            case 3:
                                member.DateOfBirth = GetDateTime("birth");
                                break;
                            case 4:
                                member.PlaceOfBirthId = Database.GetPlaceId("birth");
                                break;
                            case 5:
                                member.DateOfDeath = GetDateTime("death");
                                break;
                            case 6:
                                member.PlaceOfDeathId = Database.GetPlaceId("death");
                                break;
                            case 7:
                                member.PartnerId = Database.SetMember("partner");
                                break;
                            case 8:
                                member.FatherId = Database.SetMember("father");
                                break;
                            case 9:
                                member.MotherId = Database.SetMember("mother");
                                break;
                            default:
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                ErrorMessage("\tInvalid choice. Try again!");
                                innerExit = false;
                                break;
                        }
                    }
                } while (!innerExit);
                Database.UpdateMember(member);
            }
        }

        private static void DisplayDetails(Member member)
        {
            WriteInColor("\n\tName: ");
            Console.WriteLine(member.ToString());
            WriteInColor("\tBorn: ");
            if (member.DateOfBirth.HasValue)
            {
                Console.Write(member.DateOfBirth.Value.ToShortDateString() + " ");
            }

            if (member.PlaceOfBirthId.HasValue)
            {
                var placeOfBirth = Database.GetPlace(member.PlaceOfBirthId);
                Console.Write($"in {placeOfBirth.Item1} {placeOfBirth.Item2}.");
            }
            Console.WriteLine();

            WriteInColor("\tDeceased: ");
            if (member.DateOfDeath.HasValue) 
            { 
                Console.Write(member.DateOfDeath.Value.ToShortDateString() + " ");
            }

            if (member.PlaceOfDeathId.HasValue)
            {
                var placeOfDeath = Database.GetPlace(member.PlaceOfDeathId);
                Console.Write($"in {placeOfDeath.Item1} {placeOfDeath.Item2}.");
            }
            Console.WriteLine();

            WriteInColor("\tPartner: ");
            if (member.PartnerId.HasValue)
            {
                var partner = Database.SearchById(member.PartnerId.Value);
                Console.Write(partner.ToString());
            }
            Console.WriteLine();

            WriteInColor("\tFather: ");
            if (member.FatherId.HasValue)
            {
                var father = Database.SearchById(member.FatherId.Value);
                Console.Write(father.ToString());
            }
            Console.WriteLine();

            WriteInColor("\tMother: ");
            if (member.MotherId.HasValue)
            {
                var mother = Database.SearchById(member.MotherId.Value);
                Console.Write(mother.ToString());
            }
            Console.WriteLine();
        }

        private static bool DeleteMember(Member member)
        {
            WriteInColor($"\tAre you sure you want to delete {member}?(y/n) ", ConsoleColor.DarkRed);
            var choice = ReadLine();
            if (choice.ToLower() == "y")
            {
                Database.DeleteMember(member);
                Success(member.ToString(), "deleted");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to create the database. If it works then the tables are
        /// created and filled with starting data. If the database is not
        /// created, then none of the tables are created and no starting
        /// data is inserted.
        /// </summary>
        private void SetUp()
        {
            Console.Title = "Family Tree";
            Console.OutputEncoding = Encoding.UTF8;
            if (Database.CreateDatabase("FamilyTree"))
            {
                Database.CreateTable("Family",
                "id int PRIMARY KEY IDENTITY (1,1) NOT NULL, " +
                "first_name nvarchar(50) NOT NULL, " +
                "last_name nvarchar(50) NOT NULL, " +
                "date_of_birth nvarchar(10) NULL, " +
                "place_of_birth_id int NULL, " +
                "date_of_death nvarchar(10) NULL, " +
                "place_of_death_id int NULL, " +
                "partner_id int NULL, " +
                "father_id int NULL, " +
                "mother_id int NULL");

                Database.CreateTable("Places",
                    "id int PRIMARY KEY IDENTITY (1,1) NOT NULL, " +
                    "name nvarchar(50) NOT NULL, country_id int NOT NULL");

                Database.CreateTable("Countries",
                    "id int PRIMARY KEY IDENTITY (1,1) NOT NULL, " +
                    "name nvarchar(50) NOT NULL");

                Database.InsertFamily();
                Database.InsertPlaces();
            }
        }
    }
}