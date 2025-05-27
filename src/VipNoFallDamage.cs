﻿using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using VipCoreApi;
using static VipCoreApi.IVipCoreApi;

namespace VIP_NoFallDamage;

public class VipNoFallDamage : BasePlugin
{
    public override string ModuleAuthor => "thesamefabius, Craivyx";
    public override string ModuleName => "[VIP] No Fall Damage";
    public override string ModuleVersion => "v1.0.2";

    private IVipCoreApi? _api;
    private NoFallDamage _noFallDamage = null!;

    private PluginCapability<IVipCoreApi> PluginCapability { get; } = new("vipcore:core");

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = PluginCapability.Get();
        if (_api == null) return;

        _noFallDamage = new NoFallDamage(_api);
        _api.RegisterFeature(_noFallDamage);
    }

    public override void Unload(bool hotReload)
    {
        _noFallDamage.Dispose();
        _api?.UnRegisterFeature(_noFallDamage);
    }
}

public class NoFallDamage : VipFeatureBase, IDisposable
{
    public override string Feature => "NoFallDamage";

    public NoFallDamage(IVipCoreApi api) : base(api)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    }

    private HookResult OnTakeDamage(DynamicHook hook)
    {
        var entityInstance = hook.GetParam<CEntityInstance>(0);
        var baseEntity = new CBaseEntity(entityInstance.Handle);
        var player = GetPlayer(baseEntity);
        if (player is null) return HookResult.Continue;

        var damageInfo = hook.GetParam<CTakeDamageInfo>(1);

        var position = baseEntity.AbsOrigin;

        if (position.Z < -500.0f) 
            return HookResult.Continue;

        if (damageInfo.BitsDamageType.HasFlag(DamageTypes_t.DMG_FALL)
            && IsClientVip(player)
            && PlayerHasFeature(player)
            && GetFeatureValue<bool>(player)
            && GetPlayerFeatureState(player) is FeatureState.Enabled)
        {
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    public static CCSPlayerController? GetPlayer(CBaseEntity? ent)
    {
        if (ent != null && ent.DesignerName == "player")
        {
            var pawn = new CCSPlayerPawn(ent.Handle);
            if (!pawn.IsValid || !pawn.OriginalController.IsValid)
                return null;

            return pawn.OriginalController.Value;
        }

        return null;
    }

    public void Dispose()
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
    }
}
