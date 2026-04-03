using System;
using System.Collections.Generic;
using System.Text;

namespace Rewards_Fast2._0.Models
{
    public class Person
    {
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;

        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();

        public string LastNameDative { get; set; } = string.Empty;
        public string FirstNameDative { get; set; } = string.Empty;
        public string MiddleNameDative { get; set; } = string.Empty;

        public string FullNameDative => $"{LastNameDative} {FirstNameDative} {MiddleNameDative}".Trim();

        public string GetFullName(bool useDative)
        {
            return useDative ? FullNameDative : FullName;
        }

        public bool IsDeclined => !string.IsNullOrEmpty(LastNameDative) ||
                                   !string.IsNullOrEmpty(FirstNameDative) ||
                                   !string.IsNullOrEmpty(MiddleNameDative);
    }
}
