using System;
using System.ComponentModel;

namespace Helper;

/// <summary>
/// Преобразователь свойств: 
/// использовать вместе с PropertyRightAttribute и PropertyOrderAttribute
/// </summary>
public class PropertyConverter : ExpandableObjectConverter
{
    public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        => true;

    /// <summary>
    /// Возвращает упорядоченный список свойств c учетом прав на редактирование
    /// </summary>
    public override PropertyDescriptorCollection GetProperties(
      ITypeDescriptorContext context,
      object value,
      Attribute[] attributes) 
        => TypeDescriptor.GetProperties(value, attributes);
}

#region PropertyOrder Attribute

/// <summary>
/// Атрибут для задания сортировки, 
/// использовать вместе с PropertyConverter
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PropertyOrderAttribute(int order) : Attribute
{
    public int Order => order;
}

#endregion

#region PropertyOrderPair

/// <summary>
/// Пара имя/номер п/п с сортировкой по номеру
/// </summary>
internal class PropertyOrderPair(string name, int order) : IComparable
{
    private int _order = order;

    public string Name { get; } = name;

    /// <summary>
    /// Собственно метод сравнения
    /// </summary>
    public int CompareTo(object obj)
    {
        var otherOrder = ((PropertyOrderPair)obj)._order;

        if (otherOrder == _order)
        {
            // если Order одинаковый - сортируем по именам
            var otherName = ((PropertyOrderPair)obj).Name;
            return string.CompareOrdinal(Name, otherName);
        }
        
        if (otherOrder > _order) return -1;

        return 1;
    }
}

#endregion
