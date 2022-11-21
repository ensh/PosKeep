using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vtb.PosKeep.ReportTransfer.FileProcessing
{
    public class MonitoringConfigurationSection : ConfigurationSection
    {
        public const string SectionName = "reportMonitoring";
        public MonitoringConfigurationSection() { }

        public const string MON = "monitoring";
        [ConfigurationProperty(MON, IsRequired = true)]
        [ConfigurationCollection(typeof(MonitorCollection), AddItemName = "monitor")]
        public MonitorCollection Monitoring
        {
            get { return (MonitorCollection)base[MON]; }
        }
        public const string INT = "interval";
        [ConfigurationProperty(INT, DefaultValue = 600, IsRequired = true)]
        public int Interval
        {
            get
            {
                return (int)this[INT];
            }
        }
    }

    public class BoolElement : ConfigurationElement
    {
        public BoolElement() { }

        public const string NAME = "name";
        [ConfigurationProperty(NAME, DefaultValue = "", IsRequired = false)]
        public string Name
        {
            get { return (string)this[NAME]; }
            set { this[NAME] = value; }
        }

        public const string VALUE = "value";
        [ConfigurationProperty(VALUE, DefaultValue = false, IsRequired = true)]
        public bool Value
        {
            get { return  (bool)this[VALUE]; }
            set { this[VALUE] = value; }
        }

        public const string COMMENT = "comment";
        [ConfigurationProperty(COMMENT, DefaultValue = "", IsRequired = false)]
        public string Comment
        {
            get { return (string)this[COMMENT]; }
            set { this[COMMENT] = value; }
        }
    }

    public class StringElement : ConfigurationElement
    {
        public StringElement() { }

        public const string NAME = "name";
        [ConfigurationProperty(NAME, DefaultValue = "", IsRequired = false)]
        public string Name
        {
            get { return (string)this[NAME]; }
            set { this[NAME] = value; }
        }

        public const string VALUE = "value";
        [ConfigurationProperty(VALUE, DefaultValue = "", IsRequired = true)]
        public string Value
        {
            get { return (string)this[VALUE]; }
            set { this[VALUE] = value; }
        }

        public const string COMMENT = "comment";
        [ConfigurationProperty(COMMENT, DefaultValue = "", IsRequired = false)]
        public string Comment
        {
            get { return (string)this[COMMENT]; }
            set { this[COMMENT] = value; }
        }
    }

    public class StringCollection : ConfigurationElementCollection
    {
        public StringElement this[int index]
        {
            get { return (StringElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(value);
            }
        }

        public new StringElement this[string index]
        {
            get { return (StringElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemove(index);
                }
                BaseAdd(value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new StringElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((StringElement)element).Name;
        }
    }

    public class BoolCollection : ConfigurationElementCollection
    {
        public BoolElement this[int index]
        {
            get { return (BoolElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(value);
            }
        }

        public new BoolElement this[string index]
        {
            get { return (BoolElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemove(index);
                }
                BaseAdd(value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new BoolElement();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((BoolElement)element).Name;
        }
    }

    public class ProcessorConfigElement : ConfigurationElement
    {
        public ProcessorConfigElement() { }

        public const string NAME = "filename";
        [ConfigurationProperty(NAME, DefaultValue = "", IsRequired = true)]
        public string FileName
        {
            get { return (string)this[NAME]; }
            set { this[NAME] = value; }
        }

        public const string DIRPARAMS = "directories";
        [ConfigurationProperty(DIRPARAMS, IsRequired = true)]
        [ConfigurationCollection(typeof(StringCollection), AddItemName = "dir")]
        public StringCollection DirCollection
        {
            get { return (StringCollection)base[DIRPARAMS]; }
            set { this[DIRPARAMS] = value; }
        }

        public const string PATPARAMS = "patterns";
        [ConfigurationProperty(PATPARAMS, IsRequired = true)]
        [ConfigurationCollection(typeof(StringCollection), AddItemName = "pattern")]
        public StringCollection PatternCollection
        {
            get { return (StringCollection)base[PATPARAMS]; }
            set { this[PATPARAMS] = value; }
        }

        public const string FPARAMS = "flags";
        [ConfigurationProperty(FPARAMS, IsRequired = true)]
        [ConfigurationCollection(typeof(BoolCollection), AddItemName = "flag")]
        public BoolCollection FlagsCollection
        {
            get { return (BoolCollection)base[FPARAMS]; }
            set { this[FPARAMS] = value; }
        }
    }

    public class ProcessorCollection : ConfigurationElementCollection
    {
        public ProcessorConfigElement this[object index]
        {
            get { return (ProcessorConfigElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemove(index);
                }
                BaseAdd(value);
            }
        }
        protected override ConfigurationElement CreateNewElement()
        {
            return new ProcessorConfigElement();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ProcessorConfigElement)element).FileName;
        }
    }

    public class MonitorConfigElement : ConfigurationElement
    {
        public MonitorConfigElement() { }

        public const string NAME = "name";
        [ConfigurationProperty(NAME, DefaultValue = "*", IsRequired = false)]
        public string Name
        {
            get { return (string)this[NAME]; }
            set { this[NAME] = value; }
        }

        public const string DIRPARAMS = "directories";
        [ConfigurationProperty(DIRPARAMS, IsRequired = true)]
        [ConfigurationCollection(typeof(StringCollection), AddItemName = "dir")]
        public StringCollection DirCollection
        {
            get { return (StringCollection)base[DIRPARAMS]; }
            set { this[DIRPARAMS] = value; }
        }

        public const string PATPARAMS = "patterns";
        [ConfigurationProperty(PATPARAMS, IsRequired = true)]
        [ConfigurationCollection(typeof(StringCollection), AddItemName = "pattern")]
        public StringCollection PatternCollection
        {
            get { return (StringCollection)base[PATPARAMS]; }
            set { this[PATPARAMS] = value; }
        }

        [ConfigurationProperty("processors")]
        [ConfigurationCollection(typeof(ProcessorCollection), AddItemName = "processor")]
        public ProcessorCollection Processors
        {
            get { return (ProcessorCollection)base["processors"]; }
        }
    }

    public class MonitorCollection : ConfigurationElementCollection
    {
        public MonitorConfigElement this[object index]
        {
            get { return (MonitorConfigElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemove(index);
                }
                BaseAdd(value);
            }
        }
        protected override ConfigurationElement CreateNewElement()
        {
            return new MonitorConfigElement();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MonitorConfigElement)element).Name;
        }
    }

}
