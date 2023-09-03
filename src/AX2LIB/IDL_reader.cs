﻿using System;
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
        private void Start()
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
            NET_prototype.LIBRARY_INFO = new LIBRARY_INFO();
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
                if (IDL_string_trimmed.Contains("interface") && !IDL_string_trimmed.Contains("dispinterface") && current_marker != IDL_AREA.IDL_LIBRARY) 
                {
                    current_marker = IDL_AREA.IDL_INTERFACE;
                    if (interface_wrapper.Name != null && interface_wrapper.Name.Length > 2) NET_prototype.CLASSES.Add(interface_wrapper);
                    interface_wrapper = new CLASS_PROTOTYPE();
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

                        List<bool> are_optional = new List<bool>();
                        List< COMPONENT_PROTOTYPE.ArgumentTypes> args_types = new List<COMPONENT_PROTOTYPE.ArgumentTypes>();
                        List<string> args_names = new List<string>();


                        string temp_str_descr = "";
                        string temp_str_arg = "";
                        foreach (char ch in arguments_string)
                        {
                            if (ch == '[') local_descr_start = true;
                            if (local_descr_start && !local_descr_end) temp_str_descr += ch;
                            if (ch == ']') 
                            {
                                temp_str_descr = temp_str_descr.Replace("[", "").Replace("]", "");
                                local_descr_end = true;
                                local_arg_start = true;
                            }
                            if (local_arg_start)
                            {
                                temp_str_arg += ch;
                            }
                            if (local_arg_start && (ch == ',' || ch == ')'))
                            {
                                local_arg_end = true;
                            }

                            if (local_arg_end)
                            {
                                //data in [...]
                                string[] arg_info = new string[] { temp_str_descr };
                                if (temp_str_descr.Contains(",")) arg_info = temp_str_descr.Split(",");

                                if (arg_info.Length > 1 && arg_info[1].Contains("optional")) are_optional.Add(true);
                                else are_optional.Add(false);

                                //type of argument
                                string[] arg_type_and_name = temp_str_arg.Split(" ");
                                args_names.Add(arg_type_and_name[1]);
                                var current_type = Get_ArgumentType(arg_type_and_name[0]);
                                args_types.Add(current_type);
                                if (arg_info[0].Contains("out"))
                                {
                                    component_wrapper.ReturnedValue = current_type;
                                }
                                local_descr_end = false;
                                local_descr_end = false;
                                local_arg_start = false;

                            }
                        }

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
                if (data_string.Contains("(")) data_string = data_string.Substring(0, data_string.IndexOf("(") - 1);
                string[] arr = data_string.TrimStart().Split(" ");
                Name = arr[1];

                if (data_string.Contains(":"))
                {
                    string inherits_block = data_string.TrimStart().Substring(data_string.TrimStart().IndexOf(":"));
                    if (inherits_block.Contains("{")) inherits_block = inherits_block.Substring(0, inherits_block.IndexOf("{"));
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
                    string helpstring_value = IDL_string.Substring(IDL_string.LastIndexOf("("), IDL_string.LastIndexOf(")") - IDL_string.LastIndexOf("("));
                    helpstring_value = helpstring_value.Replace("\"", "");
                    return helpstring_value;
                }
            }
            return "";
        }
        private NET_DLL_PROTOTYPE.NET_TYPE Get_HRESULT_type (List<string> IDL_DESCRIPTION_BLOCK)
        {
            string[] arr = IDL_DESCRIPTION_BLOCK[0].Split(",");
            //is it one-string for all IDL?
            NET_DLL_PROTOTYPE.NET_TYPE type;
            if (arr[1].Contains("helpstring")) type = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_METHOD_VOID;
            else if (arr[1].Contains("propputref")) type = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_FIELD;
            else if (arr[1].Contains("propget")) type = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_METHOD_GET;
            else if (arr[1].Contains("propput")) type = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_METHOD_SET;
            else
            {
                type = NET_DLL_PROTOTYPE.NET_TYPE.TYPE_UNKNOWN;
                throw new Exception($"Can not parse HRESULT type {IDL_DESCRIPTION_BLOCK[0]}");
            }
            return type;
        }
        private COMPONENT_PROTOTYPE.ArgumentTypes Get_ArgumentType(string IDL_string)
        {
            COMPONENT_PROTOTYPE.ArgumentTypes type = COMPONENT_PROTOTYPE.ArgumentTypes.Dynamic;
            if (IDL_string.Contains("BSTR")) type = COMPONENT_PROTOTYPE.ArgumentTypes.String;
            else if (IDL_string.Contains("VARIANT")) type = COMPONENT_PROTOTYPE.ArgumentTypes.Object;
            else if (IDL_string.Contains("double")) type = COMPONENT_PROTOTYPE.ArgumentTypes.Double;
            return type;
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
        //private string GetValueS
        #endregion
    }
}