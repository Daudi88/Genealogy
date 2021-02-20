using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using static Genealogy.Helper;

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

        /// <summary>
        /// Creates a <see cref="List{T}"/> of parents
        /// based on the <paramref name="member"/>.
        /// </summary>
        /// <param name="member"></param>
        /// <returns><see cref="List{T}"/> of parents or an empty
        /// <see cref="List{T}"/> if no parents are found.</returns>
        public static List<Member> GetParents(Member member)
        {
            var parents = new List<Member>();
            var father = Database.SearchById(member.FatherId);
            if (father != null) parents.Add(father);
            var mother = Database.SearchById(member.MotherId);
            if (mother != null) parents.Add(mother);
            return parents;
        }

        /// <summary>
        /// Creates a <see cref="List{T}"/> of siblings
        /// based on the <paramref name="member"/>.
        /// </summary>
        /// <param name="member"></param>
        /// <returns><see cref="List{T}"/> of siblings or an empty
        /// <see cref="List{T}"/> if no siblings are found.</returns>
        public static List<Member> GetSiblings(Member member)
        {
            var siblings = Database.SearchByMotherOrFatherId(member.FatherId, member.MotherId);
            return siblings.Where(s => s.Id != member.Id).ToList();
        }

        /// <summary>
        /// Gets the siblings of the member's parents.
        /// </summary>
        /// <param name="member"></param>
        /// <returns><see cref="List{T}"/> of aunts and uncles or an empty list.</returns>
        public static List<Member> GetAuntsAndUncles(Member member)
        {
            var auntsAndUncles = new List<Member>();
            var parents = GetParents(member);
            foreach (var parent in parents)
            {
                auntsAndUncles.AddRange(GetSiblings(parent));
            }
            return auntsAndUncles;
        }

        /// <summary>
        /// Retrieves relatives based on the
        /// <paramref name="choice"/> made by the user.
        /// </summary>
        /// <param name="member"></param>
        /// <param name="choice"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<Member> GetRelatives(Member member, int choice, out string type)
        {
            var relatives = new List<Member>();
            switch (choice)
            {
                case 1:
                    relatives = GetParents(member);
                    type = "parents";
                    break;
                case 2:
                    relatives = Database.SearchByMotherOrFatherId(member.Id, member.Id);
                    type = "children";
                    break;
                case 3:
                    relatives.Add(Database.SearchById(member.PartnerId));
                    type = "partner";
                    break;
                case 4:
                    relatives = GetSiblings(member);
                    type = "siblings";
                    break;
                case 5:
                    var auntsAndUncles = GetAuntsAndUncles(member);
                    if (auntsAndUncles.Count > 0)
                    {
                        foreach (var auntOrUncle in auntsAndUncles)
                        {
                            var children = Database.SearchByMotherOrFatherId(auntOrUncle.Id, auntOrUncle.Id);
                            relatives.AddRange(children);
                        }
                    }
                    type = "cousins";
                    break;
                case 6:
                    relatives = GetAuntsAndUncles(member);
                    type = "aunts or uncles";
                    break;
                case 7:
                    var parents = GetParents(member);
                    foreach (var parent in parents)
                    {
                        relatives.AddRange(GetParents(parent));
                    }
                    type = "grandparents";
                    break;
                default:
                    type = "";
                    break;
            }
            return relatives;
        }

        /// <summary>
        /// Checks if the member exists in the database
        /// otherwise lets user create member.
        /// </summary>
        /// <param name="title"></param>
        /// <returns>Id of parent or 0 if no parent is found or created.</returns>
        public static int? SetMember(string title)
        {
            Console.Write($"\tEnter name of {title}: ");
            var name = ReadLine();
            if (name != "")
            {
                var members = Database.SearchByName(name);
                if (members.Count > 0)
                {
                    var id = ChooseMember(members);
                    if (id != 0)
                    {
                        return id;
                    }
                }
                name = char.ToUpper(name[0]) + name[1..];
                var choice = BigFail(name);
                if (choice.ToLower() == "y")
                {
                    return AddMember(false);
                }
            }
            return null;
        }

        /// <summary>
        /// Displays a <see cref="List{T}"/> of <see cref="Member"/> and
        /// lets the user choose a member.
        /// </summary>
        /// <param name="members"></param>
        /// <returns>Id of the <see cref="Member"/> the user chose or
        /// 0 if no <see cref="Member"/> was chosen.</returns>
        public static int ChooseMember(List<Member> members)
        {
            WriteInColor("\n\tWhich member do you want to choose?\n", ConsoleColor.Yellow);
            int ctr = 1;
            members = members.OrderBy(m => m.FirstName).ToList();
            foreach (var member in members)
            {
                Console.Write($"\t{ctr++}. {member} ");
                if (member.DateOfBirth.HasValue)
                {
                    Console.Write(member.DateOfBirth.Value.ToShortDateString());
                }
                Console.WriteLine();
            }
            WriteInColor("\t0. None of the above.\n", ConsoleColor.DarkRed);

            int id;
            while (true)
            {
                Console.Write("\t> ");
                int.TryParse(ReadLine(), out int choice);
                if (choice == 0)
                {
                    id = 0;
                    break;
                }
                else if (choice > 0 && choice <= ctr)
                {
                    id = members[choice - 1].Id;
                    break;
                }
                else
                {
                    ErrorMessage("\tInvalid choice. Try again!");
                }
            }
            return id;
        }

        /// <summary>
        /// Asks the user to choose a place. The method will loop
        /// until the user chooses a place or enters 0.
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns>The id of the chosen place or 0 if no place is chosen.</returns>
        public static int ChoosePlace(DataTable dataTable)
        {
            WriteInColor("\n\tWhich place do you want to choose?\n");
            var places = new Dictionary<int, (int, string, string)>();
            var ctr = 1;
            foreach (DataRow row in dataTable.Rows)
            {
                var id = (int)row["id"];
                var place = Database.GetPlace(id);
                places.Add(id, (ctr++, place.Item1, place.Item2));
            }
            foreach (var place in places)
            {
                Console.WriteLine($"\t{place.Value.Item1}. {place.Value.Item2} {place.Value.Item3}");
            }
            WriteInColor("\t0. None of the above\n", ConsoleColor.DarkRed);
            while (true)
            {
                Console.Write("\t> ");
                if (int.TryParse(ReadLine(), out int choice))
                {
                    if (choice == 0) return 0;
                    else if (choice <= dataTable.Rows.Count)
                    {
                        var id = places.Where(p => p.Value.Item1 == choice).First();
                        return id.Key;
                    }
                    else
                    {
                        ErrorMessage("Invalid choice. Try again!");
                    }
                }
                else
                {
                    ErrorMessage("Invalid choice. Try again!");
                }
            }
        }

        /// <summary>
        /// Lets the user add more details when creating a new member.
        /// </summary>
        /// <param name="member"></param>
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
            }

            if (MakeAChoice("\n\tDo you want to set partner(y/n)? "))
            {
                member.PartnerId = SetMember("partner");
            }

            if (MakeAChoice("\n\tDo you want to set parents(y/n)?"))
            {
                SetParents(member);
            }
            Database.UpdateMember(member);
        }

        /// <summary>
        /// Sets both parents if the user doesn't enter 0.
        /// </summary>
        /// <param name="member"></param>
        private static void SetParents(Member member)
        {
            var fatherId = SetMember("father");
            if (fatherId != 0)
            {
                member.FatherId = fatherId;
                Console.WriteLine();
            }

            var motherId = SetMember("mother");
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
                    var id = ChooseMember(members);
                    if (id != 0)
                    {
                        var member = members.Where(m => m.Id == id).FirstOrDefault();
                        SelectedMember(member);
                    }
                }
                else ErrorMessage("\tNo members matched your search.");
            }
        }

        /// <summary>
        /// Lets the user choose what data that should be missing.
        /// </summary>
        /// <returns><see cref="List{T}"/> of members that match the search.</returns>
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
                            var relatives = GetRelatives(member, option, out string type);
                            if (relatives.Count > 0)
                            {
                                var id = ChooseMember(relatives);
                                if (id != 0)
                                {
                                    var relative = Database.SearchById(id);
                                    SelectedMember(relative);
                                }
                            }
                            else ErrorMessage($"\t{member} doesn't have any {type} in the database.");
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
                                member.PartnerId = SetMember("partner");
                                break;
                            case 8:
                                member.FatherId = SetMember("father");
                                break;
                            case 9:
                                member.MotherId = SetMember("mother");
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

        /// <summary>
        /// Displays detailed information about the member. For those properties 
        /// that can be null a chech is made. If there isn't a value the 
        /// word 'unknown' is printed to the screen.
        /// </summary>
        /// <param name="member"></param>
        private static void DisplayDetails(Member member)
        {
            WriteInColor("\n\tName: ");
            Console.WriteLine(member.ToString());

            WriteInColor("\tBorn: ");
            if (member.DateOfBirth.HasValue || member.PlaceOfBirthId.HasValue)
            {
                if (member.DateOfBirth.HasValue)
                {
                    Console.Write(member.DateOfBirth.Value.ToShortDateString() + " ");
                }

                if (member.PlaceOfBirthId.HasValue)
                {
                    var placeOfBirth = Database.GetPlace(member.PlaceOfBirthId);
                    Console.Write($"in {placeOfBirth.Item1} {placeOfBirth.Item2}.");
                }
            }
            else
            {
                WriteInColor("Unknown", ConsoleColor.DarkRed);
            }
            Console.WriteLine();

            WriteInColor("\tDeceased: ");
            if (member.DateOfDeath.HasValue || member.PlaceOfDeathId.HasValue)
            {
                if (member.DateOfDeath.HasValue)
                {
                    Console.Write(member.DateOfDeath.Value.ToShortDateString() + " ");
                }

                if (member.PlaceOfDeathId.HasValue)
                {
                    var placeOfDeath = Database.GetPlace(member.PlaceOfDeathId);
                    Console.Write($"in {placeOfDeath.Item1} {placeOfDeath.Item2}.");
                }
            }
            else
            {
                WriteInColor("Unknown", ConsoleColor.DarkRed);
            }
            Console.WriteLine();

            WriteInColor("\tPartner: ");
            if (member.PartnerId.HasValue)
            {
                var partner = Database.SearchById(member.PartnerId.Value);
                Console.Write(partner.ToString());
            }
            else
            {
                WriteInColor("Unknown", ConsoleColor.DarkRed);
            }
            Console.WriteLine();

            WriteInColor("\tFather: ");
            if (member.FatherId.HasValue)
            {
                var father = Database.SearchById(member.FatherId.Value);
                Console.Write(father.ToString());
            }
            else
            {
                WriteInColor("Unknown", ConsoleColor.DarkRed);
            }
            Console.WriteLine();

            WriteInColor("\tMother: ");
            if (member.MotherId.HasValue)
            {
                var mother = Database.SearchById(member.MotherId.Value);
                Console.Write(mother.ToString());
            }
            else
            {
                WriteInColor("Unknown", ConsoleColor.DarkRed);
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Asks the user one more time if they want to delete the member. 
        /// If the user types in 'y' then the member is deleted and all 
        /// the connections to that member is removed.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        private static bool DeleteMember(Member member)
        {
            WriteInColor($"\tAre you sure you want to delete {member}?(y/n) ", ConsoleColor.DarkRed);
            var choice = ReadLine();
            if (choice.ToLower() == "y")
            {
                Database.DeleteMember(member);
                Success(member.ToString(), "deleted");
                var allMembers = Database.Search();
                foreach (var memb in allMembers)
                {
                    if (memb.PartnerId == member.Id)
                    {
                        memb.PartnerId = null;
                        Database.UpdateMember(memb);
                    }
                    else if (memb.FatherId == member.Id)
                    {
                        memb.FatherId = null;
                        Database.UpdateMember(memb);
                    }
                    else if (memb.MotherId == member.Id)
                    {
                        memb.MotherId = null;
                        Database.UpdateMember(memb);
                    }
                }
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
                "date_of_birth date NULL, " +
                "place_of_birth_id int NULL, " +
                "date_of_death date NULL, " +
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