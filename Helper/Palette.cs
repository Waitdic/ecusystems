using System.Collections.Generic;
using System.Drawing;

namespace Helper
{    
    public class Palette
    {
        /// <summary>
        /// ��������������� ������ ���: ��������-����
        /// </summary>
        protected readonly SortedList<float, int> pairs = new SortedList<float, int>();

        public bool LimitAbove { get; set; }
        public bool LimitBelow { get; set; }
        public bool IsDiscrete { get; set; }
       
        //--------------------------------------
        /// <summary>
        /// �������� ������ �������
        /// </summary>
        public void Clear()
        {
            pairs.Clear();
        }

        /// <summary>
        /// ������� ������� ������� �� ������
        /// </summary>
        /// <param name="value">�������� �������� �������</param>
        public void RemoveElement(float value)
        {
            pairs.Remove(value);
        }
        /// <summary>
        /// ������� ������� ������� �� �������
        /// </summary>
        /// <param name="index">�������� �������� �������</param>
        public void RemoveElement(int index)
        {
            if ((index < 0) && (index >= pairs.Count)) return;

            pairs.RemoveAt(index);
        }

        /// <summary>
        /// ��������  ������� ������� � ������
        /// </summary>
        /// <param name="value">�������� �������� �������</param>
        /// <param name="color">���� �������� �������</param>
        public void AddElement(float value, int color)
        {
            pairs.Add(value, color);       
        }

        //--------------------------------------
        /// <summary>
        /// �������� ���� �� �������
        /// </summary>
        /// <param name="index">������</param>
        internal int GetColor(int index)
        {
            return pairs.Values[index];
        }

        /// <summary>
        /// ���������� ���� �� �������
        /// </summary>
        /// <param name="index">������</param>
        /// <param name="color">����</param>
        internal void SetColor(int index, int color)
        {
            var value = pairs.Keys[index];
            pairs[value] = color;            
        }

        //--------------------------------------
        /// <summary>
        /// �������� �������� �� �������
        /// </summary>
        /// <param name="index">������</param>
        public float GetValue(int index)
        {
            return pairs.Keys[index];           
        }

        /// <summary>
        /// ���������� �������� �� �������
        /// </summary>
        /// <param name="index">������</param>
        /// <param name="value">��������</param>
        public void SetValue(int index, float value)
        {
            var color = pairs.Values[index];
            pairs.RemoveAt(index);            
            pairs.Add(value, color);            
        }

        //--------------------------------------
        public int Count
        {
            get { return pairs.Count; }
        }


        /// <summary>
        /// �������� ���� �� ��������
        /// </summary>
        /// <param name="value">��������</param>
        /// <param name="defaultColor">���� �� ���������</param>
        public int GetColorOnValue(float value, int defaultColor)
        {
            return InnerGetColorOnValue(value, defaultColor);
        }

        internal int InnerGetColorOnValue(float value, int defaultColor)
        {
            var count = pairs.Count;
            // ������� ������
            if (count == 0) return defaultColor;

            // --------------------------------------------------------------
            // ���� ������ �������� �������, ������� �������� ��������� value
            // --------------------------------------------------------------            

            var keys = pairs.Keys;
            var values = pairs.Values;
            var bottomIndex = FindRange(value);

            // ��������� ������ �������
            if (bottomIndex < 0)
                return LimitBelow ? defaultColor 
                    : values[0];

            // ��������� ������� �������
            var topIndex = count - 1;
            if (bottomIndex >= topIndex)
                return LimitAbove ? defaultColor
                    : values[topIndex];

            if (IsDiscrete)           
                return values[bottomIndex];

            // --------------------------------
            // ������� ����������� (MFL)
            // --------------------------------
            topIndex = bottomIndex + 1;
            // ����������� ���������
            var scale = (value - keys[bottomIndex]) / (keys[topIndex] - keys[bottomIndex]);

            // ��������� �������� �� ������            
            var bottomValue = values[bottomIndex];
            var topValue = values[topIndex];

            return DataHelper.CalcGradientColor(bottomValue, topValue, scale);
        }

        private int FindRange(float key)
        {
            var keys = pairs.Keys;
            var count = pairs.Count;

            for (var i = -1; i < count - 1; i++)
            {
                if (key >= keys[i + 1]) continue;
                return i;
            }

            return count;
        }

        public void AddElementsRange(IEnumerable<KeyValuePair<float, Color>> items)
        {
            foreach (var item in items)
            {
                pairs.Add(item.Key, item.Value.ToArgb());
            }
        }
    }    
}
