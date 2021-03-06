﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace HueLib2
{
    /// <summary>
    /// Scene Class.
    /// </summary>
    [DataContract, Serializable]
    public class Scene : HueObject
    {
        private string _name;
        /// <summary>
        /// Name of the scene.
        /// </summary>
        [DataMember, Category("Scene Properties"), Description("Name of the scene")]
        public string name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// List of the light in the scene. 
        /// </summary>
        [DataMember, Category("Scene Properties"), Description("Lights of the scene"),Browsable(false)]
        public List<string> lights {get;set;}

        /// <summary>
        /// Owner of the scene.
        /// </summary>
        [DataMember(IsRequired = false), Category("Scene Properties"), Description("Whitelist user that created or modified the content of the scene")]
        public string owner { get; set; }

        /// <summary>
        /// App specific data.
        /// </summary>
        [DataMember(IsRequired = false), Category("Scene Properties"), Description("App specific data linked to the scene."),ExpandableObject]
        public AppData appdata { get; set; }

        /// <summary>
        /// Scene can be deleted by the bridge automatically.
        /// </summary>
        [DataMember(IsRequired = false), Category("Scene Properties"), Description("Indicates whether the scene can be automatically deleted by the bridge.")]
        public bool? recycle { get; set; }

        /// <summary>
        /// Scene is Locked.
        /// </summary>
        [DataMember(IsRequired = false), Category("Scene Properties"), Description("Indicates that the scene is locked by a rule or a schedule and cannot be deleted until all resources requiring or that reference the scene are deleted.")]
        public bool? locked { get; set; }

        /// <summary>
        /// Picture path
        /// </summary>
        [DataMember(IsRequired = false), Category("Scene Properties"), Description("Path to the picture.")]
        public string picture { get; set; }

        /// <summary>
        /// Version of the scene.
        /// </summary>
        [DataMember(IsRequired = false), Category("Scene Properties"), Description("Version of scene document.")]
        public int? version { get; set; }

        /// <summary>
        /// Last time the scene was updated in UTC.
        /// </summary>
        [DataMember(IsRequired = false), Category("Scene Properties"), Description("UTC time the scene has been created or has been updated.")]
        public string lastupdated { get; set; }

        /// <summary>
        /// State of the lights in the scene.
        /// </summary>
        [DataMember(IsRequired = false), Category("Scene Properties"), Description("States of every lights in the scene."),Browsable(false)]
        public Dictionary<string, State> lightstates { get; set; }

        /// <summary>
        /// Store current light state in scene.
        /// </summary>
        [DataMember(IsRequired = false), Category("Scene Properties"), Description("Store the current light state in the scene."),Browsable(false)]
        public bool? storelightstate { get; set; }

        /// <summary>
        /// To String.
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, StringEscapeHandling = StringEscapeHandling.Default });

        }
    }
}
