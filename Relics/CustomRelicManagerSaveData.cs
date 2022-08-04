using System;
using ToolBox.Serialization;
using UnityEngine;

namespace ProLib.Relics
{
    public class CustomRelicManagerSaveData : SaveObjectData
    {
		public const String KEY = "ElementalCore.CustomRelicManager";

		public override string Name => KEY;

		[SerializeField]
		public CustomRelicSaveObject[] _relics;

		[SerializeField]
		public CustomRelicCountdownStatus[] _relicCountdowns;

		[SerializeField]
		public CustomRelicPoolSaveObject _commonRelicPool;

		[SerializeField]
		public CustomRelicPoolSaveObject _rareRelicPool;

		[SerializeField]
		public CustomRelicPoolSaveObject _bossRelicPool;

		public CustomRelicSaveObject[] Relics
		{
			get
			{
				return _relics;
			}
		}

		public CustomRelicCountdownStatus[] RelicCountdowns
		{
			get
			{
				return _relicCountdowns;
			}
		}

		public CustomRelicPoolSaveObject CommonRelicPool
		{
			get
			{
				return _commonRelicPool;
			}
		}

		public CustomRelicPoolSaveObject RareRelicPool
		{
			get
			{
				return _rareRelicPool;
			}
		}

		public CustomRelicPoolSaveObject BossRelicPool
		{
			get
			{
				return _bossRelicPool;
			}
		}


		public CustomRelicManagerSaveData(CustomRelicSaveObject[] relics, CustomRelicCountdownStatus[] relicCountdowns) : base(true)
        {
            _relics = relics;
            _relicCountdowns = relicCountdowns;
        }

		public CustomRelicManagerSaveData(CustomRelicSaveObject[] relics, CustomRelicCountdownStatus[] relicCountdowns, CustomRelicPoolSaveObject commonRelicPool, CustomRelicPoolSaveObject rareRelicPool, CustomRelicPoolSaveObject bossRelicPool) : base(true)
		{
			this._relics = relics;
			this._relicCountdowns = relicCountdowns;
			this._commonRelicPool = commonRelicPool;
			this._rareRelicPool = rareRelicPool;
			this._bossRelicPool = bossRelicPool;
		}

		public class CustomRelicCountdownStatus : ISerializable
		{
			public string RelicId;
			public int RelicCountdown;

			public CustomRelicCountdownStatus(string relicId, int relicCountdown)
			{
				RelicId = relicId;
				RelicCountdown = relicCountdown;
			}
		}

		public class CustomRelicSaveObject : ISerializable
		{
			public String RelicId;
			public int OrderOfAcquisition;
			public CustomRelicSaveObject(String relicId, int orderOfAcquisition)
			{
				RelicId = relicId;
				OrderOfAcquisition = orderOfAcquisition;
			}
		}

		public class CustomRelicPoolSaveObject : ISerializable
		{
			public CustomRelicPoolSaveObject(string[] relicIds)
			{
				RelicIds = relicIds;
			}

			public string[] RelicIds;
		}
	}
}
