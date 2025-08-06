//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Graphing.Core.Models
//{
//    public class Edge
//    {
//        public required string Id { get; init; }
//        public EdgeState State { get; set; }
//        public string? RedirectedFrom { get; set; }

//        public override bool Equals(object? obj)
//        {
//            return obj is Edge e && e.Id == Id && e.State == State && e.RedirectedFrom == RedirectedFrom;
//        }

//        public override int GetHashCode()
//        {
//            return HashCode.Combine(Id, State, RedirectedFrom);
//        }
//    }
//}
