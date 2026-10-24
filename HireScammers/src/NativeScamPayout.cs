using System;
using System.Reflection;
using HarmonyLib;
namespace HireScammers {
internal static class NativeScamPayout {
 static readonly MethodInfo payout=AccessTools.Method(typeof(KolkataOsPlayer),"ServerCreditScamPayout",new[]{typeof(Guid),typeof(int),typeof(string)})??AccessTools.Method(typeof(KolkataOsPlayer),"ServerCreditScamPayout",new[]{typeof(Guid),typeof(int)});
 static readonly MethodInfo money=AccessTools.Method(typeof(KolkataOsPlayer),"ServerCreditScamMoney",new[]{typeof(long),typeof(bool)})??AccessTools.Method(typeof(KolkataOsPlayer),"ServerCreditScamMoney",new[]{typeof(long)});
 internal static void Validate(){if(payout==null||money==null)throw new NotSupportedException("Unsupported game payout API. Update AI Scammers.");}
 internal static void Credit(KolkataOsPlayer wallet,Guid call,int amount,string scamId){Validate();payout.Invoke(wallet,payout.GetParameters().Length==3?new object[]{call,amount,scamId}:new object[]{call,amount});}
 internal static void CreditMoney(KolkataOsPlayer wallet,int amount,string scamId){Validate();money.Invoke(wallet,money.GetParameters().Length==2?new object[]{(long)amount,scamId=="ransom"}:new object[]{(long)amount});}
}
}
