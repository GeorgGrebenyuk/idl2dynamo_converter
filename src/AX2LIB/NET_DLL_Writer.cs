using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AX2LIB
{
    public class NET_DLL_Writer
    {
        private NET_DLL_PROTOTYPE proptotype;
        /// <summary>
        /// 
        /// </summary>
        private string save_path;
        private string tab = $"\t\t";
        private string LibName => proptotype.LIBRARY_INFO.Name;
        public NET_DLL_Writer(NET_DLL_PROTOTYPE proptotype, string save_path)
        {
            this.proptotype = proptotype;
            if (!Directory.Exists(save_path)) throw new DirectoryNotFoundException("Can not save to that Directory");
            this.save_path = save_path;
        }
        public void Create()
        {
            //first, let's create a new folder in save_path named "root namepsace of Library"
            save_path = Path.Combine(save_path, proptotype.LIBRARY_INFO.Name);
            if (!Directory.Exists(save_path)) Directory.CreateDirectory(save_path);
            else
            {
                Directory.Delete(save_path, true);
                Directory.CreateDirectory(save_path);
            }

            // List of interfaces name which are inherited by other interfaces (in future, create public dynamic constructor in it classes)
            List<string> inherits_info = new List<string>();
            foreach (CLASS_PROTOTYPE class_wrapper in proptotype.CLASSES)
            {
                inherits_info.Concat(class_wrapper.Inherits);
            }
            inherits_info = inherits_info.Distinct().ToList();

            foreach (CLASS_PROTOTYPE class_wrapper in proptotype.CLASSES)
            {
                StringBuilder cs_content = new StringBuilder();
                string class_name = class_wrapper.Name;
                if (class_name[0] == 'I') class_name = class_name.Substring(1);

                /*block of using nsmespaces*/

                //add namespace 
                cs_content.AppendLine($"namespace {LibName} \n" + "{");
                //add class
                cs_content.AppendLine($"{tab}public class {class_name} \n" + "{");
                //add original interface
                cs_content.AppendLine($"{tab}{tab}public {LibName}.{class_wrapper.Name} _i;");
                //add default internal constructor
                cs_content.AppendLine(
                    $"{tab}{tab}internal {class_name} (object {class_name}_object) \n" +
                    $"{tab}{tab}" + "{\n" +
                    $"{tab}{tab}{tab}" + $"this._i = {class_name}_object as {LibName}.{class_wrapper.Name};" + "\n" +
                    $"{tab}{tab}{tab}" + "if (this._i == null) throw new Exception(\"Invalid casting\");" + "\n" +
                    $"{tab}{tab}" + "}");
                if (inherits_info.Contains(class_wrapper.Name))
                {
                    //add public dynamic constructor 
                    cs_content.AppendLine(
                    $"{tab}{tab}public {class_name} (dynamic {class_name}_object_to_cast) \n" +
                    $"{tab}{tab}" + "{\n" +
                    $"{tab}{tab}{tab}" + $"this._i = {class_name}_object_to_cast._i as {LibName}.{class_wrapper.Name};" + "\n" +
                    $"{tab}{tab}{tab}" + "if (this._i == null) throw new Exception(\"Invalid casting\");" + "\n" +
                    $"{tab}{tab}" + "}");
                }
                //add other class content
                foreach (COMPONENT_PROTOTYPE class_element in class_wrapper.Members)
                {
                    string arguments_string = "";
                    string arguments_names_string = "";
                    List<string> arguments = new List<string>();
                    List<string> arguments_names = new List<string>();
                    for (int i = 0; i < class_element.ArgumentsNames.Length; i++)
                    {
                        string arg_name = class_element.ArgumentsNames[i];
                        var arg_type = class_element.ArgumentsTypes[i];
                        bool is_optional = class_element.OptionalArguments[i];
                        arguments.Add(arg_type.ToString().ToLower() + " " + arg_name);
                        arguments_names.Add(arg_name);
                    }
                    arguments_string = string.Join(",", arguments);
                    arguments_names_string = string.Join(",", arguments_names);

                    string content_type = "";
                    string element_instructions = "";
                    string element_name = class_element.Name;

                    //|| 
                    if (class_element.TYPE == NET_DLL_PROTOTYPE.NET_TYPE.TYPE_METHOD_VOID ) 
                    {
                        content_type = "void";
                        element_instructions = $"this._i.{class_element.Name}({arguments_names_string});";
                    }
                    else if (class_element.TYPE == NET_DLL_PROTOTYPE.NET_TYPE.TYPE_METHOD_SET)
                    {
                        element_name = "Set_" + element_name;
                        content_type = "void";
                        element_instructions = $"this._i.{class_element.Name} = {arguments_names_string};";
                    }
                    else if (class_element.TYPE == NET_DLL_PROTOTYPE.NET_TYPE.TYPE_METHOD_GET)
                    {
                        element_name = "Get_" + element_name;
                        content_type = class_element.ReturnedValue.ToString().ToLower();
                        string arguments_names_string2 = $"({arguments_names_string})";
                        if (arguments_names_string.Length < 2) arguments_names_string2 = "";
                        element_instructions = $"return this._i.{class_element.Name}{arguments_names_string2};";
                    }
                    else if (class_element.TYPE == NET_DLL_PROTOTYPE.NET_TYPE.TYPE_FIELD) 
                    {
                        content_type = "void";
                        element_name = "Put_" + element_name;
                        element_instructions = $"this._i.{class_element.Name}({arguments_names_string});";
                    }
                    else throw new Exception($"Invalid type of element of class {class_element.TYPE.ToString()}");

                    cs_content.AppendLine(
                        $"{tab}{tab}public {content_type} {element_name} ({arguments_string}) \n" + 
                        $"{tab}{tab}" + "{\n" + 
                        $"{tab}{tab}{tab}" + $"{element_instructions}" + "\n" + 
                        $"{tab}{tab}" + "}");
                }


                //close class
                cs_content.AppendLine($"{tab}" + "}");
                //close namespace
                cs_content.AppendLine("}");
                File.WriteAllText(Path.Combine(save_path, $"{class_name}.cs"), cs_content.ToString());
            }
        }

    }
}
