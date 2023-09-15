using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AX2LIB.NET_DLL_PROTOTYPE;

namespace AX2LIB
{
    /// <summary>
    /// Class-wrapper for duture NET DLL structure
    /// </summary>
    public class NET_DLL_PROTOTYPE
    {
        public enum NET_TYPE : int
        {
            TYPE_UNKNOWN,//need Debug
            TYPE_LIBRARY,
            TYPE_CLASS,
            TYPE_FIELD, //propputref
            TYPE_METHOD_VOID, //nothing
            TYPE_METHOD_GET,//propget
            TYPE_METHOD_SET,//propput
            TYPE_METHOD_PRIVATE_VOID, //hidden
            TYPE_CONSTRUCTOR,
            TYPE_ENUM // is it need?
        }
        public LIBRARY_INFO LIBRARY_INFO { get; internal set; }

        public List<CLASS_PROTOTYPE> CLASSES { get; internal set; }
        public List<CLASS_PROTOTYPE> Enumerations { get; internal set; }
    }
    /// <summary>
    /// Description of library
    /// The name of current library and also the name of root namespace without tabs
    /// </summary>
    public class LIBRARY_INFO : COMMON_PROTOTYPE
    {
        /// <summary>
        /// The identificator of that COM library
        /// </summary>
        public Guid GUID { get; set; }
        /// <summary>
        /// The version of that COM library
        /// </summary>
        public string VERSION { get; set; }
    }
    /// <summary>
    /// Wrapper for class content
    /// </summary>
    public class COMPONENT_PROTOTYPE : COMMON_PROTOTYPE
    {
        public enum ArgumentTypes : int
        {
            String, //BSTR
            Object, //VARIANT
            Dynamic, //any other interface
            Double, //double или *double
            Int, //int
            Long, //long
            Bool //bool

        }

        /// <summary>
        /// System type of each arguments (if exists)
        /// </summary>
        public ArgumentTypes[] ArgumentsTypes { get; set; }
        /// <summary>
        /// System type of each arguments (how wrote in IDL source. Need for enum detecting)
        /// </summary>
        public string[] ArgumentTypes_Source { get; set; }
        /// <summary>
        /// Each argument's name
        /// </summary>
        public string[] ArgumentsNames { get; set; }
        /// <summary>
        /// Flags, if arguments are optional. In default, false
        /// </summary>
        public bool[] OptionalArguments { get; set; }
        /// <summary>
        /// The type of returned value (beside cases when NET_TYPE = TYPE_METHOD_VOID)
        /// </summary>
        public ArgumentTypes ReturnedValue { get; set; }
        /// <summary>
        /// Flags, if argument is mark as 'out'
        /// </summary>
        public bool[] IsOutFlags { get; set; }
    }
    /// <summary>
    /// Wrapper for class
    /// </summary>

    public class CLASS_PROTOTYPE : COMMON_PROTOTYPE
    {
        public string[] Inherits { get; set; }
        /// <summary>
        /// The content of class
        /// </summary>
        public List<COMPONENT_PROTOTYPE> Members { get; internal set; }
    }

    /// <summary>
    /// Common description of each type (class or it's content)
    /// </summary>
    public abstract class COMMON_PROTOTYPE
    {
        /// <summary>
        /// The name of element (class, method and etc)
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The value of helpstring in IDL file
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The type of item
        /// </summary>
        public NET_TYPE TYPE { get; set; }
    }
}
