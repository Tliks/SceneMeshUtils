using nadena.dev.ndmf;
using com.aoyon.modulecreator;
using UnityEngine;

[assembly: ExportsPlugin(typeof(PluginDefinition))]

namespace com.aoyon.modulecreator
{
    public class PluginDefinition : Plugin<PluginDefinition>
    {
        public override string QualifiedName => "com.aoyon.modulecreator";

        public override string DisplayName => "ModuleCreator";

        protected override void Configure()
        {
            var sequence =
                InPhase(BuildPhase.Transforming)
                .BeforePlugin("net.rs64.tex-trans-tool")
                .BeforePlugin("com.anatawa12.avatar-optimizer");

            sequence.Run(RemoveMeshFromScenePass.Instance);
        }
    }
}