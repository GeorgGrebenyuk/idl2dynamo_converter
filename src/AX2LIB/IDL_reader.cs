using System.Runtime.CompilerServices;

namespace AX2LIB
{
    /// <summary>
    /// Class for reading single IDL file and converting it to NET_DLL_PROTOTYPE
    /// </summary>
    public class IDL_reader
    {
        /// <summary>
        /// Temporal content of IDL file
        /// </summary>
        private List<string> IDL_file_data;

        public NET_DLL_PROTOTYPE NET_prototype;
        public IDL_reader (string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException ("IDL file not exist!");
            }
            this.IDL_file_data = File.ReadAllLines (path).ToList();
        }
        private void Start()
        {
            this.NET_prototype = new NET_DLL_PROTOTYPE();
            //read library section
            var IDL_section_library = GetElements(ref this.IDL_file_data)[0];
            NET_prototype.LIBRARY_INFO = new LIBRARY_INFO();
            string Name_info;
            List<string> Inherits_info;
            ParseName(IDL_section_library.Name, out Name_info, out Inherits_info);
            NET_prototype.LIBRARY_INFO.Name = Name_info;
            NET_prototype.LIBRARY_INFO.GUID = GetGuid(IDL_section_library.Description);
            NET_prototype.LIBRARY_INFO.Description = GetHelpstring(IDL_section_library.Description);

            //read library content
            //first, read to temp List<string> info about interfaces in that Library
            bool read_inherits = true;
            List<string> lib_interfaces = new List<string>();
            for (int i = 0; i < IDL_section_library.Content.Count; i++)
            {
                string IDL_Content_one_string = IDL_section_library.Content[i];
                if (IDL_Content_one_string.Contains("interface") && !IDL_Content_one_string.Contains("dispinterface") && read_inherits)
                {
                    string interface_name = IDL_Content_one_string.TrimStart().Split(" ").Last();
                    interface_name = interface_name.Substring(0, interface_name.IndexOf(";"));
                    lib_interfaces.Add(interface_name);
                }
                if (IDL_Content_one_string.Contains("[")) read_inherits = false;


            }



        }
        #region IDL_STRUCTURE_PARSER
        private enum IDL_AREA : int
        {
            IDL_DESCRIPTION, //data in [...]
            IDL_DEFINITION, // data in {...}
            IDL_MEMBER_DEFINITION //data between 'HRESULT' and ';'
        }
        private struct IDL_ELEMENT
        {
            public List<string> Description; //Get_IDL_Area(IDL_AREA.IDL_DESCRIPTION)
            public List<string> Name; //data between Description and Content
            public List<string> Content; //Get_IDL_Area(IDL_AREA.IDL_MEMBER_DEFINITION or IDL_AREA.IDL_DEFINITION)

        }
        /// <summary>
        /// Get interface info or library info (name, inherits info)
        /// </summary>
        /// <param name="name_string">IDL_ELEMENT.Name</param>
        private void ParseName(List<string> data, out string Name, out List<string> Inherits)
        {
            Inherits = new List<string>();
            Name = null;
            //In fact,there is only one string in 'data'
            foreach (string one_string in data)
            {
                if (one_string.Contains("library") || one_string.Contains("interface"))
                {
                    string[] arr = one_string.TrimStart().Split(" ");
                    Name = arr[1];

                    if (one_string.Contains(":"))
                    {
                        string inherits_block = one_string.TrimStart().Substring(one_string.TrimStart().IndexOf(":"));
                        if (inherits_block.Contains("{")) inherits_block = inherits_block.Substring(0, inherits_block.IndexOf("{"));
                        string[] inherits_data;
                        if (inherits_block.Contains(",")) inherits_data = inherits_block.Split(",");
                        else inherits_data = new string[1] { inherits_block };

                        Inherits = inherits_data.ToList();
                    }
                } 
            }
            if (Name == null)
            {
                throw new Exception($"Can not parse string {string.Join("\t", data)}");
            }
        }
        private List<IDL_ELEMENT> GetElements(ref List<string> IDL_data)
        {
            int lines_counter = 0;
            List<IDL_ELEMENT> elements = new List<IDL_ELEMENT>();

            IDL_ELEMENT element = GetElement(ref IDL_data, ref lines_counter);
            void check_and_run()
            {

            }


            return elements;
        }
        private IDL_ELEMENT GetElement(ref List<string> IDL_data, ref int IDL_lines_counter)
        {
            IDL_ELEMENT element = new IDL_ELEMENT();
            element.Description = Get_IDL_Area(IDL_AREA.IDL_DESCRIPTION, ref IDL_data, ref IDL_lines_counter);
            int lines_counter_start = IDL_lines_counter;

            element.Content = Get_IDL_Area(IDL_AREA.IDL_DEFINITION, ref IDL_data, ref IDL_lines_counter);

            element.Name = new List<string>();
            for (int i = lines_counter_start; i <= IDL_data.Count; i++)
            {
                string one_string = IDL_data[i];
                element.Name.Add(one_string);
                if (one_string.Contains("{") || one_string.Contains(";")) break;
            }
            return element;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="IDL_data"></param>
        /// <param name="IDL_lines_counter">Start index of string to read data</param>
        /// <returns></returns>
        private List<string> Get_IDL_Area(IDL_AREA type, ref List<string> IDL_data, ref int IDL_lines_counter)
        {
            bool first_block = false;
            bool end_block = false;
            string first_checking_symbol = "";
            string end_checking_symbol="";

            switch (type)
            {
                case IDL_AREA.IDL_DESCRIPTION:
                    first_checking_symbol = "[";
                    end_checking_symbol = "]";
                    break;
                case IDL_AREA.IDL_DEFINITION:
                    first_checking_symbol = "{";
                    end_checking_symbol = "}";
                    break;
                case IDL_AREA.IDL_MEMBER_DEFINITION:
                    first_checking_symbol = "HRESULT";
                    end_checking_symbol = ";";
                    break;
            }

            List<string> that_text_block = new List<string>();
            int temp_counter_blocks = 0;
            //Conter for reader lines of IDL file
            //int IDL_lines_counter = 0;

            for (int i = IDL_lines_counter; i < IDL_data.Count; i++)
            {
                IDL_lines_counter += 1;
                string current_IDL_line = IDL_data[i];
                if (current_IDL_line.Contains(first_checking_symbol) && !first_block) first_block = true;
                //if there is a nested block (f.e. nested interface in global library's content
                else if (current_IDL_line.Contains(first_checking_symbol) && first_block && !end_block) temp_counter_blocks += 1;

                if (first_block && !end_block) that_text_block.Add(current_IDL_line);
                if (current_IDL_line.Contains(end_checking_symbol) && temp_counter_blocks == 0 && !end_block)
                {
                    end_block = true;
                    break;
                }
                else if (current_IDL_line.Contains(end_checking_symbol) && temp_counter_blocks > 0) temp_counter_blocks -= 1;
            }
            //lines_counter = IDL_lines_counter;
            return that_text_block;
        }
        /// <summary>
        /// Getting a helpstring's attribute value ot nothing if helpstring no present
        /// </summary>
        /// <param name="IDL_DESCRIPTION_BLOCK">IDL_DESCRIPTION block of IDL file for that element</param>
        /// <returns></returns>
        private string GetHelpstring(List<string> IDL_DESCRIPTION_BLOCK)
        {
            foreach (string IDL_string in IDL_DESCRIPTION_BLOCK)
            {
                if (IDL_string.Contains("helpstring"))
                {
                    string helpstring_value = IDL_string.Substring(IDL_string.LastIndexOf("("), IDL_string.LastIndexOf(")") - IDL_string.LastIndexOf("("));
                    helpstring_value = helpstring_value.Replace("\"", "");
                    return helpstring_value;
                }
            }
            return "";
        }
        private Guid GetGuid(List<string> IDL_DESCRIPTION_BLOCK)
        {
            Guid guid = Guid.Empty;
            foreach (string IDL_string in IDL_DESCRIPTION_BLOCK)
            {
                if (IDL_string.Contains("uuid"))
                {
                    string uuid_value = IDL_string.Substring(IDL_string.LastIndexOf("("), IDL_string.LastIndexOf(")") - IDL_string.LastIndexOf("("));
                    //uuid_value = helpstring_value.Replace("\"", "");
                    Guid.TryParse(uuid_value, out guid);
                }
            }
            if (guid == Guid.Empty)
            {
                throw new Exception("Can not parse IDL uuid");
            }
            return guid;
        }
        #endregion
    }
}