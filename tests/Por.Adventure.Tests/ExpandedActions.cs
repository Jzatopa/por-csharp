using System;
using Por.Core;
using static Por.Core.Types;
static class ExpandedActions
{
    static int count;
    static void Check(bool value,string name){count++;if(!value)throw new Exception("Expanded action: "+name);}
    public static int Run()
    {
        var prior=GDVars.creatureList;var priorCur=GDVars.curChar;byte priorIfc=GDVars.ifcState;
        var message=Combat.NameMessage;
        try
        {
            // The actual status algorithm is exercised; only display text is recorded.
            int announcements=0;Combat.NameMessage=(c,s,color,pause)=>announcements++;
            var enemy=new TCharRecord{hostile=1,status=C_Status.DYING,combatAddr=new TCombatRec{notPcNpc=1,overkill=4}};
            var first=new TCharRecord{hostile=0,status=C_Status.DYING,hpCur=0,enabled=0,combatAddr=new TCombatRec{notPcNpc=0,overkill=7}};
            var second=new TCharRecord{hostile=0,status=C_Status.DYING,hpCur=0,enabled=0,combatAddr=new TCombatRec{notPcNpc=0,overkill=3}};
            enemy.nextCharAddr=first;first.nextCharAddr=second;GDVars.creatureList=enemy;
            Check(Combat.BandageDying(false),"detect dying party member");
            Check(first.status==5&&second.status==5&&announcements==0,"detection does not bandage");
            Check(Combat.BandageDying(true),"native bandage finds ally");
            Check(first.status==4&&first.combatAddr.overkill==0,"first ally stabilized/reset bleeding");
            Check(first.hpCur==0&&first.enabled==0,"bandage neither heals nor revives");
            Check(enemy.status==5&&enemy.combatAddr.overkill==4,"hostile ignored");
            Check(second.status==5&&second.combatAddr.overkill==3&&announcements==1,"one ally per action");
            Combat.BandageDying(true);Check(second.status==4&&second.combatAddr.overkill==0&&announcements==2,"second action stabilizes second ally");
            Check(!Combat.BandageDying(false),"no remaining eligible dying ally");
            GDVars.ifcState=C_Ifc.COMBAT;
            var patient=new TCharRecord{name="CAP",hpCur=26,hpMax=28,status=C_Status.OKAY,enabled=1};GDVars.curChar=patient;
            Check(FxEngine.RestoreHitPoints(patient,8,false)&&patient.hpCur==28,"healing clips at maximum HP");
            FxEngine.RestoreHitPoints(patient,8,false);Check(patient.hpCur==28,"already full cannot overflow");
            patient.hpCur=0;patient.status=C_Status.DYING;patient.enabled=0;
            Check(FxEngine.RestoreHitPoints(patient,3,false)&&patient.hpCur==3&&patient.status==C_Status.UNCONSCIOUS&&patient.enabled==0,"combat cure stabilizes without bypassing wake lifecycle");
            Console.WriteLine("Expanded action boundaries: "+count+" assertions PASS");return count;
        }
        finally{Combat.NameMessage=message;GDVars.creatureList=prior;GDVars.curChar=priorCur;GDVars.ifcState=priorIfc;}
    }
}
