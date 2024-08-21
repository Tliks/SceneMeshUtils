using System.Collections.Generic;
using System;
using UnityEngine;

namespace com.aoyon.modulecreator
{
    [Serializable]
    public class TriangleSelectionContainer : ScriptableObject
    {
        public Mesh mesh;
        public List<TriangleSelection> selections = new List<TriangleSelection>();
    }

    [Serializable]
    public class TriangleSelection
    {
        public List<int> selection = new List<int>();

        public string displayname;
        public long createtime;
    }
}

