using System;
using System.Collections.Generic;

namespace Core.UI.FormSubmit.Fields
{
    /// <summary>
    /// Definition for a form field
    /// </summary>
    [Serializable]
    public class FormFieldDefinition
    {
        public string name;
        public string label;
        public string type;
        public object defaultValue;
        public bool required;
        public string placeholder;
        public string tooltip;
        public Dictionary<string, object> options; // For select fields, validation rules, etc.
        
        public FormFieldDefinition()
        {
            options = new Dictionary<string, object>();
        }
        
        public FormFieldDefinition(string name, string label, string type, object defaultValue = null)
        {
            this.name = name;
            this.label = label;
            this.type = type;
            this.defaultValue = defaultValue;
            this.options = new Dictionary<string, object>();
        }
    }
}