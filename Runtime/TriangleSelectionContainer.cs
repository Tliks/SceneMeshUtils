using System.Collections.Generic;
using System;
using UnityEngine;

namespace com.aoyon.scenemeshutils
{
    [Serializable]
    public class TriangleSelectionContainer : ScriptableObject
    {
        public Mesh mesh;
        public int TriangleCount;
        public List<TriangleSelection> selections = new();
    }

    [Serializable]
    public class TriangleSelection
    {
        public List<int> selection = new();

        public string displayname = "";
        public long createtime = 0;
    }
}

