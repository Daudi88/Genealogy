using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace Genealogy
{
    internal class SqlDatabase
    {
        public string Connection { get; set; } = @"Data Source = .\SQLExpress; Integrated Security = true; database = {0}";
        public string DatabaseName { get; set; } = "FamilyTree";
        public DataTable DataTable { get; set; } = new DataTable();
        public string Query { get; set; }       

        /// <summary>
        /// Tries to create a database with the provided <paramref name="name"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <returns><see langword="true"/> if the database was
        /// created, otherwise <see langword="false"/>.</returns>
        public bool CreateDatabase(string name)
        {
            try
            {
                DatabaseName = "master";
                ExecuteSql($"CREATE DATABASE {name}");
                DatabaseName = name;
                Utility.WriteDelayed("\n\tDatabase created! Welcome to the Family Tree!\n");
                return true;
            }
            catch (Exception)
            {
                DatabaseName = name;
                Utility.WriteDelayed("\n\tWelcome back to the Family Tree!\n");
                return false;
            }
        }

        /// <summary>
        /// Creates a table based on the <paramref name="name"/>
        /// and <paramref name="columns"/> provided.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="columns"></param>
        public void CreateTable(string name, string columns)
        {
            ExecuteSql($"CREATE TABLE {name} ({columns})");
            //ExecuteSql($"");
        }

        /// <summary>
        /// Inserts start data to fill up the Family Tree.
        /// </summary>
        /// <param name="file"></param>
        public void InsertFamily()
        {
            var lines = File.ReadAllLines(Path.Combine(
                Environment.CurrentDirectory, @$"..\..\..\..\Family.txt"));
            foreach (var line in lines)
            {
                CreateFullMember(GetMember(line));
            }
        }

        /// <summary>
        /// Reads a file and for every line in the file
        /// creates a place in the Places table.
        /// </summary>
        public void InsertPlaces()
        {
            var lines = File.ReadAllLines(Path.Combine(
                Environment.CurrentDirectory, @$"..\..\..\..\Places.txt"));
            foreach (var line in lines)
            {
                var place = line.Split(", ");
                CreatePlace(place);
            }
        }

        /// <summary>
        /// Inserts a new <paramref name="member"/> into the Family table
        /// with all the values.
        /// </summary>
        /// <param name="member"></param>
        public void CreateFullMember(Member member)
        {
            Query = "SET IDENTITY_INSERT Family ON; INSERT Family " +
                "(id, first_name, last_name, date_of_birth, place_of_birth_id, " +
                "date_of_death, place_of_death_id, partner_id, father_id, mother_id) " +
                "VALUES (@id, @fName, @lName, @dBirth, @pBirth, " +
                "@dDeath, @pDeath, @pId, @fId, @mId)";
            var parameters = GetParameters(member);
            ExecuteSql(Query, parameters);
        }

        /// <summary>
        /// Inserts a new <paramref name="member"/> into the Family table
        /// with some basic values.
        /// </summary>
        /// <param name="member"></param>
        public void CreateBasicMember(Member member)
        {
            Query = "INSERT Family (first_name, last_name, date_of_birth, place_of_birth_id) " +
                "VALUES (@fName, @lName, @dBirth, @pBirth)";
            var parameters = GetParameters(member);
            ExecuteSql(Query, parameters);
        }

        /// <summary>
        /// Creates a <see cref="List{T}"/> of <see cref="Member"/>
        /// based on the <paramref name="name"/> provided.
        /// </summary>
        /// <param name="name"></param>
        /// <returns><see cref="List{T}"/> of <see cref="Member"/> or an empty
        /// <see cref="List{T}"/> if no members are found.</returns>
        public List<Member> SearchByName(string name)
        {
            if (name.Contains(" "))
            {
                var names = name.Split(" ");
                Query = "SELECT * FROM Family WHERE first_name " +
                    "LIKE @firstName AND last_name LIKE @lastName";
                DataTable = GetDataTable(Query,
                    ("@firstName", $"%{names[0]}%"), ("@lastName", $"%{names[1]}%"));
            }
            else
            {
                Query = "SELECT * FROM Family WHERE first_name " +
                    "LIKE @name OR last_name LIKE @name";
                DataTable = GetDataTable(Query, ("@name", $"%{name}%"));
            }

            var members = new List<Member>();
            if (DataTable.Rows.Count > 0)
            {
                foreach (DataRow row in DataTable.Rows)
                {
                    members.Add(GetMember(row));
                }
            }
            return members;
        }

        /// <summary>
        /// Retrieves member based on id.
        /// </summary>
        /// <param name="member"></param>
        /// <returns><see cref="Member"/> or <see langword="null"/>.</returns>
        public Member SearchById(int? id)
        {
            Query = "SELECT * FROM Family WHERE id = @id";
            DataTable = GetDataTable(Query,
                ("@id", id.ToString()));
            if (DataTable.Rows.Count > 0)
            {
                return GetMember(DataTable.Rows[0]);
            }
            return null;
        }

        /// <summary>
        /// Selects all members based on date_of_birth.
        /// </summary>
        /// <param name="year"></param>
        /// <returns><see cref="List{T}"/> of <see cref="Member"/>.</returns>
        public List<Member> SearchByYear(string year)
        {
            Query = "SELECT * FROM Family WHERE date_of_birth LIKE @year";
            DataTable = GetDataTable(Query, ("@year", $"{year}%"));
            var members = new List<Member>();
            if (DataTable.Rows.Count > 0)
            {
                foreach (DataRow row in DataTable.Rows)
                {
                    members.Add(GetMember(row));
                }
            }
            return members;
        }

        /// <summary>
        /// Takes all the members values and updates the table at the database.
        /// </summary>
        /// <param name="member"></param>
        public void UpdateMember(Member member)
        {
            Query = $"UPDATE Family SET first_name = @fName, " +
                $"last_name = @lName, date_of_birth = @dBirth, " +
                $"place_of_birth_id = @pBirth, date_of_death = @dDeath," +
                $"place_of_death_id = @pDeath, partner_id = @pId, " +
                $"mother_id = @mId, father_id = @fId WHERE id = @id";
            var parameters = GetParameters(member);
            ExecuteSql(Query, parameters);
        }

        /// <summary>
        /// Deletes a member from the database.
        /// </summary>
        /// <param name="member"></param>
        public void DeleteMember(Member member)
        {
            Query = "DELETE FROM Family WHERE id = @id";
            ExecuteSql(Query, ("@id", member.Id.ToString()));
        }

        /// <summary>
        /// Checks if the member in question exists in the database.
        /// </summary>
        /// <param name="name"></param>
        /// <returns><see langword="true"/> if the member exists otherwise
        /// <see langword="false"/>.</returns>
        public bool DoesMemberExist(string name)
        {
            var members = SearchByName(name);
            return members.Count > 0;
        }

        /// <summary>
        /// Creates a <see cref="List{T}"/> of parents
        /// based on the <paramref name="member"/>.
        /// </summary>
        /// <param name="member"></param>
        /// <returns><see cref="List{T}"/> of parents or an empty
        /// <see cref="List{T}"/> if no parents are found.</returns>
        public List<Member> GetParents(Member member)
        {
            var parents = new List<Member>();
            var father = SearchById(member.FatherId);
            if (father != null) parents.Add(father);
            var mother = SearchById(member.MotherId);
            if (mother != null) parents.Add(mother);
            return parents;
        }

        /// <summary>
        /// Creates a <see cref="List{T}"/> of childrens
        /// based on the <paramref name="member"/>.
        /// </summary>
        /// <param name="member"></param>
        /// <returns><see cref="List{T}"/> of children or an empty
        /// <see cref="List{T}"/> if no children are found.</returns>
        public List<Member> GetChildren(Member member)
        {
            Query = "SELECT * FROM Family WHERE mother_id = @id " +
                "OR father_id = @id";
            DataTable = GetDataTable(Query,
                ("@id", member.Id.ToString()));
            var children = new List<Member>();
            if (DataTable.Rows.Count > 0)
            {
                foreach (DataRow row in DataTable.Rows)
                {
                    children.Add(GetMember(row));
                }
            }
            return children;
        }

        /// <summary>
        /// Creates a <see cref="List{T}"/> of siblings
        /// based on the <paramref name="member"/>.
        /// </summary>
        /// <param name="member"></param>
        /// <returns><see cref="List{T}"/> of siblings or an empty
        /// <see cref="List{T}"/> if no siblings are found.</returns>
        public List<Member> GetSiblings(Member member)
        {
            Query = "SELECT * FROM Family WHERE mother_id = @motherId " +
                "OR father_id = @fatherId";
            DataTable = GetDataTable(Query,
                ("@motherId", member.MotherId.ToString()),
                ("@fatherId", member.FatherId.ToString()));

            List<Member> siblings = new List<Member>();
            if (DataTable.Rows.Count > 0)
            {
                foreach (DataRow row in DataTable.Rows)
                {
                    siblings.Add(GetMember(row));
                }
            }
            return siblings.Where(s => s.Id != member.Id).ToList();
        }

        /// <summary>
        /// Creates a <see cref="List{T}"/> of cousins
        /// based on the <paramref name="member"/>.
        /// </summary>
        /// <param name="member"></param>
        /// <returns><see cref="List{T}"/> of cousins or an empty
        /// <see cref="List{T}"/> if no cousins are found.</returns>
        public List<Member> GetCousins(Member member)
        {
            var auntsAndUncles = GetAuntsAndUncles(member);
            List<Member> cousins = new List<Member>();
            if (auntsAndUncles.Count > 0)
            {
                foreach (var auntOrUncle in auntsAndUncles)
                {
                    var children = GetChildren(auntOrUncle);
                    cousins.AddRange(children);
                }
            }
            return cousins;
        }

        /// <summary>
        /// Creates a <see cref="List{T}"/> of aunts and uncles. Retrieves the parents
        /// of the <see langword="member"/> and then the parents' siblings.
        /// </summary>
        /// <param name="member"></param>
        /// <returns><see cref="List{T}"/> of aunts and uncles or an empty
        /// <see cref="List{T}"/> if no aunts or uncles are found.</returns>
        public List<Member> GetAuntsAndUncles(Member member)
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
        /// Creates a <see cref="List{T}"/> of grandparents. Retireves parents
        /// of <see langword="member"/> and then the parents' parents.
        /// </summary>
        /// <param name="member"></param>
        /// <returns><see cref="List{T}"/> of grandparents or an empty
        /// list if no grandparents are found.</returns>
        public List<Member> GetGrandparents(Member member)
        {
            var grandparents = new List<Member>();
            var parents = GetParents(member);
            foreach (var parent in parents)
            {
                grandparents.AddRange(GetParents(parent));
            }
            return grandparents;
        }

        /// <summary>
        /// Retrieves relatives based on the
        /// <paramref name="choice"/> made by the user.
        /// </summary>
        /// <param name="member"></param>
        /// <param name="choice"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<Member> GetRelatives(Member member, int choice, out string type)
        {
            var relatives = new List<Member>();
            switch (choice)
            {
                case 1:
                    relatives = GetParents(member);
                    type = "parents";
                    break;

                case 2:
                    relatives = GetChildren(member);
                    type = "children";
                    break;

                case 3:
                    relatives.Add(SearchById(member.PartnerId));
                    type = "partner";
                    break;

                case 4:
                    relatives = GetSiblings(member);
                    type = "siblings";
                    break;

                case 5:
                    relatives = GetCousins(member);
                    type = "cousins";
                    break;

                case 6:
                    relatives = GetAuntsAndUncles(member);
                    type = "aunts or uncles";
                    break;

                case 7:
                    relatives = GetGrandparents(member);
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
        public int? SetMember(string title)
        {
            Console.Write($"\tEnter name of {title}: ");
            var name = Utility.ReadLine();
            if (name != "")
            {
                var members = SearchByName(name);
                if (members.Count > 0)
                {
                    var id = ChooseMember(members);
                    if (id != 0)
                    {
                        return id;
                    }
                }
                name = char.ToUpper(name[0]) + name[1..];
                var choice = Utility.BigFail(name);
                if (choice.ToLower() == "y")
                {
                    return FamilyTree.AddMember(false);
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
        public int ChooseMember(List<Member> members)
        {
            Utility.WriteInColor("\n\tWhich member do you want to choose?\n", ConsoleColor.Yellow);
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
            Utility.WriteInColor("\t0. None of the above.\n", ConsoleColor.DarkRed);

            int id;
            while (true)
            {
                Console.Write("\t> ");
                if (int.TryParse(Utility.ReadLine(), out int choice))
                {
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
                        Utility.ErrorMessage("\tInvalid choice. Try again!");
                    }
                }
                else
                {
                    Utility.ErrorMessage("\tInvalid choice. Try again!");
                }
            }
            return id;
        }

        /// <summary>
        /// Retrieves place and country based in <paramref name="placeId"/>.
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns>The place and country or empty strings if no place is found.</returns>
        public (string, string) GetPlace(int? placeId)
        {
            if (placeId.HasValue)
            {
                Query = "SELECT name, country_id FROM Places WHERE id = @place_id";
                DataTable = GetDataTable(Query, ("@place_id", placeId.Value.ToString()));
                var place = DataTable.Rows[0]["name"].ToString();
                Query = "SELECT name FROM Countries WHERE id = @country_id";
                DataTable = GetDataTable(Query, ("@country_id", DataTable.Rows[0]["country_id"].ToString()));
                var country = DataTable.Rows[0]["name"].ToString();
                return (place, country);
            }
            return ("", "");
        }

        /// <summary>
        /// Promts the user to enter a place. Then checks if the
        /// place exists in the database.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>If the place exists the place id is
        /// returned, otherwise 0.</returns>
        public int? GetPlaceId(string type)
        {
            Console.Write($"\tEnter place of {type}: ");
            var place = Utility.ReadLine();
            if (place == "0") return 0;
            else if (place != "")
            {
                place = char.ToUpper(place[0]) + place[1..];
                Query = "SELECT * FROM Places WHERE name LIKE @place";
                DataTable = GetDataTable(Query, ("@place", $"%{place}%"));
                if (DataTable.Rows.Count > 0)
                {
                    var id = ChoosePlace(DataTable);
                    if (id != 0) return id;
                }

                var choice = Utility.BigFail(place);
                if (choice.ToLower() == "y")
                {
                    var id = CreatePlace();
                    return id;
                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the last added id from any specified <paramref name="table"/>.
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public int GetLastAddedId(string table)
        {
            string name;
            if (table == "Family") name = "Family";
            else if (table == "Places") name = "Places";
            else name = "Countries";
            DataTable = GetDataTable($"SELECT id FROM {name}");
            var ids = new List<int>();
            foreach (DataRow row in DataTable.Rows)
            {
                ids.Add(int.Parse(row["id"].ToString()));
            }
            return ids.Last();
        }

        /// <summary>
        /// Selects all members from the Family table. Allows possible <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public List<Member> Search(string filter = "")
        {
            DataTable = GetDataTable("SELECT * FROM Family " + filter);
            List<Member> members = new List<Member>();
            if (DataTable.Rows.Count > 0)
            {
                foreach (DataRow row in DataTable.Rows)
                {
                    members.Add(GetMember(row));
                }
                return members;
            }
            return null;
        }

        /// <summary>
        /// Takes a string representing a member. Separates the data into an array.
        /// AAssigns every part to its member property. The perts that can be
        /// <see langword="null"/> are checked before assignment.
        /// </summary>
        /// <param name="line"></param>
        /// <returns>A <see cref="Member"/> with all its properties assigned.</returns>
        public Member GetMember(string line)
        {
            var data = line.Split(", ");
            var member = new Member
            {
                Id = int.Parse(data[0]),
                FirstName = data[1],
                LastName = data[2]
            };
            if (data[3] != "null") member.DateOfBirth = Convert.ToDateTime(data[3]);
            if (data[4] != "null") member.PlaceOfBirthId = int.Parse(data[4]);
            if (data[5] != "null") member.DateOfDeath = Convert.ToDateTime(data[5]);
            if (data[6] != "null") member.PlaceOfDeathId = int.Parse(data[6]);
            if (data[7] != "null") member.PartnerId = int.Parse(data[7]);
            if (data[8] != "null") member.FatherId = int.Parse(data[8]);
            if (data[9] != "null") member.MotherId = int.Parse(data[9]);
            return member;
        }

        /// <summary>
        /// Executes an SQL command based on the <paramref name="query"/> query
        /// and the <paramref name="parameters"/> provided.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private int ExecuteSql(string query, params (string, string)[] parameters)
        {
            var connectionString = string.Format(Connection, DatabaseName);
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.Item1, parameter.Item2);
                    }

                    foreach (SqlParameter parameter in command.Parameters)
                    {
                        if (parameter.Value == null)
                        {
                            parameter.Value = DBNull.Value;
                        }
                    }
                    return command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Retrieves data from the database based on the <paramref name="query"/>
        /// and <paramref name="parameters"/> provided.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameters"></param>
        /// <returns>DataTable with all the data or empty if no data was found.</returns>
        private DataTable GetDataTable(string query, params (string, string)[] parameters)
        {
            var dataTable = new DataTable();
            var connectionString = string.Format(Connection, DatabaseName);
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue(parameter.Item1, parameter.Item2);
                    }

                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            return dataTable;
        }

        /// <summary>
        /// Asks the user to choose a place. The method will loop
        /// until the user chooses a place or enters 0.
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns>The id of the chosen place or 0 if no place is chosen.</returns>
        private int ChoosePlace(DataTable dataTable)
        {
            Utility.WriteInColor("\n\tWhich place do you want to choose?\n");
            var places = new Dictionary<int, (int, string, string)>();
            var ctr = 1;
            foreach (DataRow row in dataTable.Rows)
            {
                var id = (int)row["id"];
                var place = GetPlace(id);
                places.Add(id, (ctr++, place.Item1, place.Item2));
            }
            foreach (var place in places)
            {
                Console.WriteLine($"\t{place.Value.Item1}. {place.Value.Item2} {place.Value.Item3}");
            }
            Utility.WriteInColor("\t0. None of the above\n", ConsoleColor.DarkRed);
            while (true)
            {
                Console.Write("\t> ");
                if (int.TryParse(Utility.ReadLine(), out int choice))
                {
                    if (choice == 0) return 0;
                    else if (choice <= dataTable.Rows.Count)
                    {
                        var id = places.Where(p => p.Value.Item1 == choice).First();
                        return id.Key;
                    }
                    else
                    {
                        Utility.ErrorMessage("Invalid choice. Try again!");
                    }
                }
                else
                {
                    Utility.ErrorMessage("Invalid choice. Try again!");
                }
            }
        }

        /// <summary>
        /// Creates a new place based on the place provided.
        /// If country doesn't exist, country is also created.
        /// </summary>
        /// <returns>Last added id from the Places table.</returns>
        private int CreatePlace(string place = null, string country = null)
        {
            if (place == null && country == null)
            {
                Console.WriteLine();
                place = Utility.GetName("place");
                country = Utility.GetName("country");
                Utility.Success(place);
            }
            var countryId = GetCountryId(country);
            if (countryId == 0)
            {
                Query = "INSERT Countries (name) VALUES (@name)";
                ExecuteSql(Query, ("@name", country));
                countryId = GetLastAddedId("Countries");
            }
            Query = "INSERT Places (name, country_id) VALUES (@name, @countryId)";
            ExecuteSql(Query, ("@name", place), ("@countryId", countryId.ToString()));
            return GetLastAddedId("Places");
        }

        private void CreatePlace(string[] place)
        {
            var countryId = GetCountryId(place[2]);
            if (countryId == 0)
            {
                Query = "INSERT Countries (name) VALUES (@name)";
                ExecuteSql(Query, ("@name", place[2]));
                countryId = GetLastAddedId("Countries");
            }

            Query = "SET IDENTITY_INSERT Places ON; " +
                "INSERT Places (id, name, country_id)" +
                "VALUES (@id, @name, @countryId)";

            var parameters = new (string, string)[]
            {
                ("@id", place[0]),
                ("@name", place[1]),
                ("@countryId", countryId.ToString())
            };

            ExecuteSql(Query, parameters);

        }

        /// <summary>
        /// Gets the id of a country using its <paramref name="name"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The id of a country or 0 if no match.</returns>
        private int GetCountryId(string name)
        {
            Query = "SELECT * FROM Countries WHERE name LIKE @name";
            DataTable = GetDataTable(Query, ("@name", $"%{name}%"));
            if (DataTable.Rows.Count > 0)
            {
                return (int)DataTable.Rows[0]["id"];
            }
            return 0;
        }       

        /// <summary>
        /// Takes a <see cref="DataRow"/> and turns it into a <see cref="Member"/>.
        /// Checks the properties that can be <see cref="DBNull"/> and sets them
        /// to <see langword="null"/> if they are.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private Member GetMember(DataRow row)
        {
            var member = new Member
            {
                Id = (int)row["id"],
                FirstName = row["first_name"].ToString(),
                LastName = row["last_name"].ToString(),
            };
            if (!(row["date_of_birth"] is DBNull)) member.DateOfBirth = Convert.ToDateTime(row["date_of_birth"]);
            if (!(row["place_of_birth_id"] is DBNull)) member.PlaceOfBirthId = (int)row["place_of_birth_id"];
            if (!(row["date_of_death"] is DBNull)) member.DateOfDeath = Convert.ToDateTime(row["date_of_death"]);
            if (!(row["place_of_death_id"] is DBNull)) member.PlaceOfDeathId = (int)row["place_of_death_id"];
            if (!(row["partner_id"] is DBNull)) member.PartnerId = (int)row["partner_id"];
            if (!(row["father_id"] is DBNull)) member.FatherId = (int)row["father_id"];
            if (!(row["mother_id"] is DBNull)) member.MotherId = (int)row["mother_id"];
            return member;
        }

        /// <summary>
        /// Converts every property from the member into a paramterer. Checks
        /// if the properties that can be <see langword="null"/> are
        /// <see langword="null"/> or not before assigning them to the parameter list.
        /// </summary>
        /// <param name="member"></param>
        /// <returns>An array of <see cref="Tuple"/> with the parameter "keys"
        /// and their values.</returns>
        private (string, string)[] GetParameters(Member member)
        {
            var dateOfBirth = member.DateOfBirth != null ? member.DateOfBirth.Value.ToString("d") : null;
            var placeOfBirthId = member.PlaceOfBirthId != null ? member.PlaceOfBirthId.Value.ToString() : null;
            var dateOfDeath = member.DateOfDeath != null ? member.DateOfDeath.Value.ToString("d") : null;
            var placeOfDeathId = member.PlaceOfDeathId != null ? member.PlaceOfDeathId.Value.ToString() : null;
            var partnerId = member.PartnerId != null ? member.PartnerId.Value.ToString() : null;
            var fatherId = member.FatherId != null ? member.FatherId.Value.ToString() : null;
            var motherId = member.MotherId != null ? member.MotherId.Value.ToString() : null;

            return new (string, string)[]
            {
                ("@id", member.Id.ToString()),
                ("@fName", member.FirstName),
                ("@lName", member.LastName),
                ("@dBirth", dateOfBirth),
                ("@pBirth", placeOfBirthId),
                ("@dDeath", dateOfDeath),
                ("@pDeath", placeOfDeathId),
                ("@pId", partnerId),
                ("@fId", fatherId),
                ("@mId", motherId)
            };
        }
    }
}