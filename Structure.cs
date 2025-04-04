namespace PYVS_MeltshopDataCollector
{
    partial class Program
    {
        //meltshop operation data
        public struct MeltshopData
        {
            public string heat;            //heat number   
            public string porder;          //pre-order number
            public short strand;           //strand number (not use)
            public short seq;              //sequence number (default = 1)

            public int eafEE_pre;          //EAF Preparation step electric energy
            public int eafEE_melt1;        //EAF Melting 1 step electric energy
            public int eafEE_melt2;        //EAF Melting 2 step electric energy
            public int eafEE_melt3;        //EAF Melting 3 step electric energy
            public int eafEE_meltExtra;    //EAF Melting Extra step electric energy
            public int eafEE_ref;          //EAF Refining step electric energy

            public int eafModuleFuel_pre;      //EAF Preparation step total module fuel(NG)
            public int eafModuleFuel_melt1;    //EAF Melting 1 step total module fuel(NG)
            public int eafModuleFuel_melt2;    //EAF Melting 2 step total module fuel(NG)
            public int eafModuleFuel_melt3;    //EAF Melting 3 step total module fuel(NG)
            public int eafModuleFuel_meltExtra;//EAF Melting Extra steptotal module fuel(NG)
            public int eafModuleFuel_ref;      //EAF Refining step total module fuel(NG)


            public void InitData()
            {
                this.heat = null;
                this.porder = null;
                this.strand = 0;
                this.seq = 0;
                this.eafEE_pre = 0;
                this.eafEE_melt1 = 0;
                this.eafEE_melt2 = 0;
                this.eafEE_melt3 = 0;
                this.eafEE_meltExtra = 0;
                this.eafEE_ref = 0;
                this.eafModuleFuel_pre = 0;
                this.eafModuleFuel_melt1 = 0;
                this.eafModuleFuel_melt2 = 0;
                this.eafModuleFuel_melt3 = 0;
                this.eafModuleFuel_meltExtra = 0;
                this.eafModuleFuel_ref = 0;
            }
        }

    }
}
