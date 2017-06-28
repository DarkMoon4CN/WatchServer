using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRV.Model
{
    public class User
    {
        public Byte rt { get; set; }
        public String serverip { get; set; }
        public UInt16 serverport { get; set; }
        public UInt32 userid { get; set; }
        public String username { get; set; }
        public String truename { get; set; }
        public Int32 classid { get; set; }
        public Int32 schoolid { get; set; }
        public String classname { get; set; }
        public String schoolname { get; set; }
        public String educode { get; set; }
        public Boolean sex { get; set; }
        public Byte age { get; set; }
        public Int32 height { get; set; }
        public Int32 weight { get; set; }
        public Int32 sportmode { get; set; }
    }
}