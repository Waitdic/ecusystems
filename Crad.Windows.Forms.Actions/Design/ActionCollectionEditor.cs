using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections.Generic;

namespace Crad.Windows.Forms.Actions.Design;

internal sealed class ActionCollectionEditor : CollectionEditor
{
    private Type[] _returnedTypes;

    public ActionCollectionEditor() : base(typeof(ActionCollection)) {}

    protected override Type[] CreateNewItemTypes()
    {
        return _returnedTypes;
    }
    public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
    {
        _returnedTypes ??= GetReturnedTypes(provider);

        return base.EditValue(context, provider, value);
    }

    private static Type[] GetReturnedTypes(IServiceProvider provider)
    {
        var res = new List<Type>();

        var tds = (ITypeDiscoveryService)provider.GetService(typeof(ITypeDiscoveryService));

        if (tds == null) return res.ToArray();
        
        foreach (Type actionType in tds.GetTypes(typeof(Action), false))
        {
            if (actionType.GetCustomAttributes(typeof(StandardActionAttribute), false).Length > 0 &&
                !res.Contains(actionType))
                res.Add(actionType);
        }

        return res.ToArray();
    }
}
