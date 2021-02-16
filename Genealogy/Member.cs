using System;

namespace Genealogy
{
    internal class Member
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? DateOfBirth { get; set; } = null;
        public int? PlaceOfBirthId { get; set; } = null;
        public DateTime? DateOfDeath { get; set; } = null;
        public int? PlaceOfDeathId { get; set; } = null;
        public int? PartnerId { get; set; } = null;
        public int? FatherId { get; set; } = null;
        public int? MotherId { get; set; } = null;

        public override string ToString()
        {
            return FirstName + " " + LastName;
        }
    }
}