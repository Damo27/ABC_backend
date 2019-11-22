//___________________________________Model contains both an Item property and list<Item> property______________
//____________________________________________Used in views which require both properties_______________

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ABC_backend.Models
{
    public class ParentModel
    {
        public Item Item { get; set; }
        public IEnumerable<Item> Items { get; set; }
    }
}