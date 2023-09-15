using System;
using System.Linq;
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
        public void Start()
        {
            this.NET_prototype = new NET_DLL_PROTOTYPE();
            //read library section
            bool start_description_block = false; // '['
            bool end_description_block = false; // ']'
            IDL_AREA current_marker = IDL_AREA.IDL_UNKNOWN;
            List<string> temp_storage_description = new List<string>();
            List<string> temp_storage_definition = new List<string>();
            List<string> temp_storage_inherits = new List<string>();
            List<string> lib_interfaces = new List<string>();
            string temp_element_name;
            int temp_blocks_counter = 0;
            bool temp_enum_start = false;
            bool temp_enum_end = false;
            NET_prototype.LIBRARY_INFO = new LIBRARY_INFO();
            NET_prototype.LIBRARY_INFO.TYPE = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_LIBRARY;
            NET_prototype.CLASSES = new List<CLASS_PROTOTYPE>();
            NET_prototype.Enumerations = new List<CLASS_PROTOTYPE>();
            CLASS_PROTOTYPE interface_wrapper = new CLASS_PROTOTYPE();
            COMPONENT_PROTOTYPE component_wrapper = new COMPONENT_PROTOTYPE();

            foreach (string IDL_string in IDL_file_data)
            {
                string IDL_string_trimmed = IDL_string.TrimStart();


                //parse info about library
                if (IDL_string_trimmed.Contains("library")) 
                {
                    current_marker = IDL_AREA.IDL_LIBRARY;
                    NET_prototype.LIBRARY_INFO.GUID = GetGuid(temp_storage_description);
                    ParseName(IDL_string_trimmed, out temp_element_name, out temp_storage_inherits);
                    NET_prototype.LIBRARY_INFO.Name = temp_element_name;
                    NET_prototype.LIBRARY_INFO.TYPE = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_LIBRARY;
                    NET_prototype.LIBRARY_INFO.Description = GetHelpstring(temp_storage_description);

                }
                if (current_marker == IDL_AREA.IDL_LIBRARY && IDL_string_trimmed.Contains("[")) temp_blocks_counter += 1;
               
                if (current_marker == IDL_AREA.IDL_LIBRARY && temp_blocks_counter == 0 &&
                    IDL_string_trimmed.Contains("interface") && !IDL_string_trimmed.Contains("dispinterface"))
                {
                    string interface_name = IDL_string_trimmed.TrimStart().Split(" ").Last();
                    interface_name = interface_name.Substring(0, interface_name.IndexOf(";"));
                    lib_interfaces.Add(interface_name);
                    lib_interfaces = lib_interfaces.Distinct().ToList();
                }
                //go to other sections
                if (current_marker == IDL_AREA.IDL_LIBRARY && temp_blocks_counter > 0) current_marker = IDL_AREA.IDL_UNKNOWN;

                if (current_marker != IDL_AREA.IDL_LIBRARY && IDL_string_trimmed.Contains("enum")) temp_enum_start = true;
                if (temp_enum_start && IDL_string_trimmed.Contains("}"))
                {
                    temp_enum_start = false;
                    string enum_name = IDL_string_trimmed.Substring(IDL_string_trimmed.IndexOf("}") + 1);
                    enum_name = enum_name.Substring(0, enum_name.IndexOf(';'));
                    var enum_info = new CLASS_PROTOTYPE();
                    enum_info.Name = enum_name;
                    enum_info.TYPE = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_ENUM;
                    enum_info.Description = "";
                    NET_prototype.Enumerations.Add(enum_info);
                }
                
                if (!IDL_string_trimmed.Contains("helpstring") && IDL_string_trimmed.Contains("interface") && !IDL_string_trimmed.Contains("dispinterface") && current_marker != IDL_AREA.IDL_LIBRARY) 
                {
                    current_marker = IDL_AREA.IDL_INTERFACE;
                    if (interface_wrapper.Name != null && interface_wrapper.Name.Length > 2) NET_prototype.CLASSES.Add(interface_wrapper);
                    interface_wrapper = new CLASS_PROTOTYPE();
                    interface_wrapper.TYPE = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_CLASS;
                    interface_wrapper.Members = new List<COMPONENT_PROTOTYPE>();
                    ParseName(IDL_string_trimmed, out temp_element_name, out temp_storage_inherits);
                    interface_wrapper.Inherits = temp_storage_inherits.ToArray();
                    interface_wrapper.Name = temp_element_name;
                    interface_wrapper.Description = GetHelpstring(temp_storage_description);
                    //temp_storage_definition.Add(IDL_string.TrimStart());
                }
                if (IDL_string_trimmed.Contains("HRESULT")) 
                {
                    component_wrapper = new COMPONENT_PROTOTYPE();
                    temp_storage_definition = new List<string>();
                    current_marker = IDL_AREA.IDL_HRESULT;
                    ParseName(IDL_string_trimmed, out temp_element_name, out temp_storage_inherits);
                    component_wrapper.Name = temp_element_name;
                    component_wrapper.Description = GetHelpstring(temp_storage_description);
                    component_wrapper.TYPE = Get_HRESULT_type(temp_storage_description);
                }
                if (current_marker == IDL_AREA.IDL_HRESULT) 
                {
                    temp_storage_definition.Add(IDL_string_trimmed);
                    if (IDL_string_trimmed.Contains(";"))
                    {
                        current_marker = IDL_AREA.IDL_UNKNOWN;
                        

                        //parse HRESULT
                        string arguments_string = string.Join(" ", temp_storage_definition);
                        arguments_string = arguments_string.Substring(arguments_string.IndexOf("("));
                        arguments_string = arguments_string.Substring(0, arguments_string.LastIndexOf(")") + 1);

                        bool local_descr_start = false;
                        bool local_descr_end = false;

                        bool local_arg_start = false;
                        bool local_arg_end = false;
                        //int once_contains_close_bracet = 0;

                        List<bool> are_optional = new List<bool>();
                        List<bool> are_out = new List<bool>();
                        List< COMPONENT_PROTOTYPE.ArgumentTypes> args_types = new List<COMPONENT_PROTOTYPE.ArgumentTypes>();
                        List<string> args_names = new List<string>();
                        List<string> args_types_source = new List<string>();


                        string temp_str_descr = "";
                        string temp_str_arg = "";

                        foreach (char ch in arguments_string)
                        {
                            if (ch == '[') 
                            {
                                local_descr_start = true;
                                local_descr_end = false;
                            }
                            if (local_descr_start && !local_descr_end) temp_str_descr += ch;
                            if (ch == ']') 
                            {
                                temp_str_descr = temp_str_descr.Replace("[", "").Replace("]", "");
                                local_descr_end = true;
                                local_arg_start = true;
                            }
                            //Exception if there is 'SAFEARRAY(VARIANT)' in argument's string
                            //if (ch == ')' && temp_str_arg.Contains("SAFEARRAY") && once_contains_close_bracet == 0) once_contains_close_bracet = 1;
                            //else if ((ch == ')' || ch == ',') && temp_str_arg.Contains("SAFEARRAY") && once_contains_close_bracet == 1) once_contains_close_bracet = 2;

                            if (local_arg_start && ch != ']' && ch != ',')
                            {
                                if ((ch == ')' && temp_str_arg.Contains("SAFEARRAY(")) || (ch != ')' && !temp_str_arg.Contains("SAFEARRAY("))) temp_str_arg += ch;
                            }
                            if (local_arg_start && (ch == ',' || ch == ')'))
                            {
                                //if (once_contains_close_bracet == 1) continue;
                                //if (once_contains_close_bracet == 0 || once_contains_close_bracet == 2)
                                local_arg_end = true;
                            }
                            if (local_arg_end && temp_str_arg.Contains("SAFEARRAY(")) local_arg_end = false;
                            if (local_arg_end)
                            {
                                //data in [...]
                                string[] arg_info = new string[] { temp_str_descr };
                                if (temp_str_descr.Contains(",")) arg_info = temp_str_descr.Split(",");



                                //type of argument
                                string[] arg_type_and_name = temp_str_arg.TrimStart().Split(" ");
                                
                                var current_type = Get_ArgumentType(arg_type_and_name[0]);
                                //bool is_out_argument = false;
                                if (arg_info[0].Contains("out") && arg_info.Length > 1 && arg_info[1].Contains("retval"))// && arg_info[0].Contains("retval")
                                {
                                    component_wrapper.ReturnedValue = current_type;
                                    component_wrapper.TYPE = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_METHOD_GET;
                                }
                                else
                                {
                                    if (arg_info[0].Contains("out")) are_out.Add(true);
                                    else are_out.Add(false);

                                    args_names.Add(arg_type_and_name[1]);
                                    args_types.Add(current_type);
                                    args_types_source.Add(temp_str_arg);

                                    if (arg_info.Length > 1 && arg_info[1].Contains("optional")) are_optional.Add(true);
                                    else are_optional.Add(false);
                                }
                                local_descr_end = false;
                                local_descr_end = false;
                                local_arg_start = false;
                                local_arg_end = false;
                                //once_contains_close_bracet = 0;

                                temp_str_descr = "";
                                temp_str_arg = "";
                            }

                        }


                        component_wrapper.ArgumentTypes_Source = args_types_source.ToArray();
                        component_wrapper.IsOutFlags = are_out.ToArray();
                        component_wrapper.OptionalArguments = are_optional.ToArray();
                        component_wrapper.ArgumentsNames = args_names.ToArray();
                        component_wrapper.ArgumentsTypes = args_types.ToArray();
                        interface_wrapper.Members.Add(component_wrapper);
                    }
                }


                //get description block in the end because in HRESULT there are same blocks for arguments
                if (IDL_string_trimmed.Contains("["))
                {
                    temp_storage_description = new List<string>();
                    start_description_block = true;
                    end_description_block = false;
                }
                if (start_description_block && !end_description_block) temp_storage_description.Add(IDL_string_trimmed);
                if (IDL_string_trimmed.Contains("]")) end_description_block = true;



            }
            
        }
        #region IDL_STRUCTURE_PARSER
        private enum IDL_AREA : int
        {
            IDL_UNKNOWN,
            IDL_LIBRARY,
            IDL_INTERFACE,
            IDL_HRESULT
        }
        /// <summary>
        /// Get interface info or library info (name, inherits info)
        /// </summary>
        /// <param name="name_string">IDL_ELEMENT.Name</param>
        private void ParseName(string data_string, out string Name, out List<string> Inherits)
        {
            Inherits = new List<string>();
            Name = null;
            //In fact,there is only one string in 'data'
            if (data_string.Contains("library") || data_string.Contains("interface") || data_string.Contains("HRESULT"))
            {
                if (data_string.Contains("(")) data_string = data_string.Substring(0, data_string.IndexOf("("));
                string[] arr = data_string.TrimStart().Split(" ");
                Name = arr[1];

                if (data_string.Contains(":"))
                {
                    string inherits_block = data_string.TrimStart().Substring(data_string.TrimStart().IndexOf(":"));
                    if (inherits_block.Contains("{")) inherits_block = inherits_block.Substring(0, inherits_block.IndexOf("{"));
                    inherits_block = inherits_block.Replace(": ", "");
                    string[] inherits_data;
                    if (inherits_block.Contains(",")) inherits_data = inherits_block.Split(",");
                    else inherits_data = new string[1] { inherits_block };

                    Inherits = inherits_data.ToList();
                }

            }
            if (Name == null)
            {
                throw new Exception($"Can not parse string {data_string}");
            }
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
                    string helpstring_value = GetValueInBracets(IDL_string);//IDL_string.Substring(IDL_string.LastIndexOf("("), IDL_string.LastIndexOf(")") - IDL_string.LastIndexOf("("));
                    helpstring_value = helpstring_value.Replace("\"", "");
                    return helpstring_value;
                }
            }
            return "";
        }
        private NET_DLL_PROTOTYPE.NET_TYPE Get_HRESULT_type (List<string> IDL_DESCRIPTION_BLOCK)
        {
            string need_string = IDL_DESCRIPTION_BLOCK[0];
            NET_DLL_PROTOTYPE.NET_TYPE type;
            if (need_string.Contains(","))
            {
                string[] arr = IDL_DESCRIPTION_BLOCK[0].Split(",");
                //is it one-string for all IDL?
                
                if (arr[1].Contains("helpstring") || arr[1].Contains("helpcontext") || arr[1].Contains("restricted")) type = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_METHOD_VOID;
                else if (arr[1].Contains("propputref")) type = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_FIELD;
                else if (arr[1].Contains("propget") || arr[1].Contains("vararg")) type = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_METHOD_GET; //vararg
                else if (arr[1].Contains("propput")) type = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_METHOD_SET;
                else if (arr[1].Contains("hidden")) type = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_METHOD_PRIVATE_VOID;
                else
                {
                    type = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_UNKNOWN;
                    throw new Exception($"Can not parse HRESULT type {IDL_DESCRIPTION_BLOCK[0]}");
                }
            }
            else
            {
                type = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_METHOD_VOID;
                //throw new Exception($"No types and helpstring for element {need_string}");
            }
            return type;

        }
        private COMPONENT_PROTOTYPE.ArgumentTypes Get_ArgumentType(string IDL_string)
        {
            COMPONENT_PROTOTYPE.ArgumentTypes type = COMPONENT_PROTOTYPE.ArgumentTypes.Dynamic;
            if (IDL_string.Contains("BSTR")) type = COMPONENT_PROTOTYPE.ArgumentTypes.String;
            else if (IDL_string.Contains("BOOL")) type = COMPONENT_PROTOTYPE.ArgumentTypes.Bool;
            else if (IDL_string.Contains("VARIANT")) type = COMPONENT_PROTOTYPE.ArgumentTypes.Object;
            else if (Char.IsUpper(IDL_string.TrimStart()[0]))
            {
                //this is I... (some intrface)
            }
            else if (IDL_string.Contains("double")) type = COMPONENT_PROTOTYPE.ArgumentTypes.Double;
            else if (IDL_string.Contains("int")) type = COMPONENT_PROTOTYPE.ArgumentTypes.Int;
            else if (IDL_string.Contains("long")) type = COMPONENT_PROTOTYPE.ArgumentTypes.Long;
            return type;
        }
        private Guid GetGuid(List<string> IDL_DESCRIPTION_BLOCK)
        {
            Guid guid = Guid.Empty;
            foreach (string IDL_string in IDL_DESCRIPTION_BLOCK)
            {
                if (IDL_string.Contains("uuid"))
                {
                    string uuid_value = GetValueInBracets(IDL_string);
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
        private string GetValueInBracets(string IDL_string)
        {
            return IDL_string.Substring(IDL_string.LastIndexOf("(") + 1, IDL_string.LastIndexOf(")") - IDL_string.LastIndexOf("(") - 1);
        }
        #endregion
    }
}