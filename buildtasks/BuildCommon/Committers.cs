using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuildCommon
{
    public enum UpdateType
    {
        None = 0,
        Patch = 1,
        Minor = 2,
        Major = 3,
        NewService = 4
    }

    public class Committer
    {
        public static List<Committer> ValidCommitters = new List<Committer>
        {
            new Committer( "Milind Gokarn", "gokarnm@amazon.com" ),
            new Committer( "Jim Flanagan", "jimfl@amazon.com" ),
            new Committer( "Norm Johanson", "normj@amazon.com" ),
            new Committer( "Pavel Safronov", "pavel@amazon.com" ),
            new Committer( "Steve Roberts", "strobe@amazon.com" ),
            new Committer( "Sattwik Pati", "sattwikp@amazon.com" ),
            new Committer( "Steven Kang", "stevkang@amazon.com" ),
            new Committer( "John Vellozzi", "vellozzi@amazon.com" ),
            new Committer( "Karthik Saligrama", "saligram@amazon.com" ),
            new Committer( "aws-sdk-dotnet-automation", "github-aws-sdk-dotnet-automation@amazon.com" )
        };

        public static Committer FindCommitter(string name)
        {
            var committer = ValidCommitters.FirstOrDefault(x => string.Equals(x.Name, name));
            if (committer == null)
                throw new Exception("Failed to find commiter " + name);
            return committer;
        }


        public string Name { get; set; }
        public string Email { get; set; }

        public Committer()
        { }
        public Committer(string name, string email)
        {
            Name = name;
            Email = email;
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(Name))
                throw new InvalidOperationException();
            if (string.IsNullOrEmpty(Email))
                throw new InvalidOperationException();
        }

        public override bool Equals(object obj)
        {
            var otherCommitter = obj as Committer;
            if (otherCommitter == null)
                return false;

            return (
                string.Equals(this.Name, otherCommitter.Name, StringComparison.Ordinal) &&
                string.Equals(this.Name, otherCommitter.Name, StringComparison.Ordinal));
        }
        public override int GetHashCode()
        {
            return
                (Name ?? "").GetHashCode() ^
                (Email ?? "").GetHashCode();
        }
    }
}
