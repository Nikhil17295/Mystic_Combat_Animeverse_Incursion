using System;
using System.IO;
using UnityEngine;
using MysticCombat.Fighter;
using MysticCombat.Core;

namespace MysticCombat.Network
{
    /// <summary>
    /// Packet type identifiers for the Mystic Combat LAN protocol.
    /// </summary>
    public enum PacketType : byte
    {
        Handshake = 1,
        CharSelectSync = 2,
        MatchStart = 3,
        FighterInput = 4,
        MatchSnapshot = 5,
        RoundReset = 6,
        MatchEnd = 7,
        Heartbeat = 8,
        Disconnect = 9
    }

    /// <summary>
    /// Compact 9-byte packet carrying active player inputs over the network.
    /// Bitmask stores 7 discrete fighter action buttons in a single byte!
    /// </summary>
    public struct FighterInputPacket
    {
        public Vector2 MoveInput;
        public bool JumpPressed;
        public bool CrouchHeld;
        public bool LightAttackPressed;
        public bool HeavyAttackPressed;
        public bool SkillPressed;
        public bool UltimatePressed;
        public bool MeditateHeld;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(MoveInput.x);
            writer.Write(MoveInput.y);

            byte buttons = 0;
            if (JumpPressed) buttons |= (1 << 0);
            if (CrouchHeld) buttons |= (1 << 1);
            if (LightAttackPressed) buttons |= (1 << 2);
            if (HeavyAttackPressed) buttons |= (1 << 3);
            if (SkillPressed) buttons |= (1 << 4);
            if (UltimatePressed) buttons |= (1 << 5);
            if (MeditateHeld) buttons |= (1 << 6);

            writer.Write(buttons);
        }

        public static FighterInputPacket Deserialize(BinaryReader reader)
        {
            FighterInputPacket packet = new FighterInputPacket();
            packet.MoveInput = new Vector2(reader.ReadSingle(), reader.ReadSingle());

            byte buttons = reader.ReadByte();
            packet.JumpPressed = (buttons & (1 << 0)) != 0;
            packet.CrouchHeld = (buttons & (1 << 1)) != 0;
            packet.LightAttackPressed = (buttons & (1 << 2)) != 0;
            packet.HeavyAttackPressed = (buttons & (1 << 3)) != 0;
            packet.SkillPressed = (buttons & (1 << 4)) != 0;
            packet.UltimatePressed = (buttons & (1 << 5)) != 0;
            packet.MeditateHeld = (buttons & (1 << 6)) != 0;

            return packet;
        }
    }

    /// <summary>
    /// Compact snapshot of a single fighter's physical and mechanical state.
    /// Used by Host to synchronize Client visual presentation.
    /// </summary>
    public struct FighterSnapshot
    {
        public float PositionX;
        public float PositionY;
        public float VelocityX;
        public float VelocityY;
        public short Health;
        public short Ki;
        public FighterState State;
        public bool FlipX;
        public byte AnimStateIndex;
        public byte AnimFrame;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(PositionX);
            writer.Write(PositionY);
            writer.Write(VelocityX);
            writer.Write(VelocityY);
            writer.Write(Health);
            writer.Write(Ki);
            writer.Write((byte)State);
            writer.Write(FlipX);
            writer.Write(AnimStateIndex);
            writer.Write(AnimFrame);
        }

        public static FighterSnapshot Deserialize(BinaryReader reader)
        {
            FighterSnapshot s = new FighterSnapshot();
            s.PositionX = reader.ReadSingle();
            s.PositionY = reader.ReadSingle();
            s.VelocityX = reader.ReadSingle();
            s.VelocityY = reader.ReadSingle();
            s.Health = reader.ReadInt16();
            s.Ki = reader.ReadInt16();
            s.State = (FighterState)reader.ReadByte();
            s.FlipX = reader.ReadBoolean();
            s.AnimStateIndex = reader.ReadByte();
            s.AnimFrame = reader.ReadByte();
            return s;
        }
    }

    /// <summary>
    /// Complete match snapshot sent by Host at 60Hz.
    /// Provides 100% authoritative synchronization of positions, HUD, and state.
    /// </summary>
    public struct MatchSnapshotPacket
    {
        public byte CurrentRound;
        public float RoundTimer;
        public byte MatchState;
        public byte P1RoundWins;
        public byte P2RoundWins;
        public FighterSnapshot P1;
        public FighterSnapshot P2;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(CurrentRound);
            writer.Write(RoundTimer);
            writer.Write(MatchState);
            writer.Write(P1RoundWins);
            writer.Write(P2RoundWins);
            P1.Serialize(writer);
            P2.Serialize(writer);
        }

        public static MatchSnapshotPacket Deserialize(BinaryReader reader)
        {
            MatchSnapshotPacket p = new MatchSnapshotPacket();
            p.CurrentRound = reader.ReadByte();
            p.RoundTimer = reader.ReadSingle();
            p.MatchState = reader.ReadByte();
            p.P1RoundWins = reader.ReadByte();
            p.P2RoundWins = reader.ReadByte();
            p.P1 = FighterSnapshot.Deserialize(reader);
            p.P2 = FighterSnapshot.Deserialize(reader);
            return p;
        }
    }

    /// <summary>
    /// Synchronizes character selection over LAN.
    /// </summary>
    public struct CharSelectSyncPacket
    {
        public byte PlayerIndex; // 1 for Host, 2 for Client
        public string CharacterId;
        public bool IsLockedIn;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(PlayerIndex);
            writer.Write(CharacterId ?? string.Empty);
            writer.Write(IsLockedIn);
        }

        public static CharSelectSyncPacket Deserialize(BinaryReader reader)
        {
            CharSelectSyncPacket p = new CharSelectSyncPacket();
            p.PlayerIndex = reader.ReadByte();
            p.CharacterId = reader.ReadString();
            p.IsLockedIn = reader.ReadBoolean();
            return p;
        }
    }

    /// <summary>
    /// Host tells Client to load combat scene with final locked-in characters.
    /// </summary>
    public struct MatchStartPacket
    {
        public string P1CharacterId;
        public string P2CharacterId;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(P1CharacterId ?? "goku");
            writer.Write(P2CharacterId ?? "saitama");
        }

        public static MatchStartPacket Deserialize(BinaryReader reader)
        {
            MatchStartPacket p = new MatchStartPacket();
            p.P1CharacterId = reader.ReadString();
            p.P2CharacterId = reader.ReadString();
            return p;
        }
    }
}
