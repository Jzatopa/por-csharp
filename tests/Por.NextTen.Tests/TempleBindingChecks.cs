// Checks that the desktop startup path installs Temple.Effects, and that the installed adapter passes calls to FxEngine/Magic.
using System;
using Por.Core;

static partial class Tests
{
    static void TempleBindingInstalled()
    {
        var saved = Temple.Effects;
        try
        {
            Temple.Effects = null;
            FinalGameplayBindings.Bind();
            Yes(Temple.Effects != null, "FinalGameplayBindings.Bind installs Temple.Effects");
            var c = MkChar("T", 50, 5, 9, 1, 0);
            Yes(!Temple.Effects.HasDisease(c), "no disease on a clean character through the binding");
            Temple.Effects.RemoveDiseases(c);
            Temple.Effects.RemoveEffect(c, 0, null);
        }
        finally { Temple.Effects = saved; }
    }
}
