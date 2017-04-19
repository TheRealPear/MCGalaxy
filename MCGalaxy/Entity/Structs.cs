﻿/*
    Copyright 2015 MCGalaxy
    
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;

namespace MCGalaxy {
	
    /// <summary> Represents the position of an entity in the world. </summary>
    public struct Position: IEquatable<Position> {
        
        /// <summary> X fixed-point location in the world. </summary>
        public int X;
        
        /// <summary> Y fixed-point location in the world. (vertical) </summary>
        public int Y;
        
        /// <summary> Z fixed-point location in the world. </summary>
        public int Z;
        
        
        public Position(int x, int y, int z) { X = x; Y = y; Z = z; }
        
        public static Position FromFeet(int x, int y, int z) { return new Position(x, y + Entities.CharacterHeight, z); }
        
        /// <summary> World/block coordinate of this position. </summary>
        public Vec3S32 BlockCoords { get { return new Vec3S32(X >> 5, Y >> 5, Z >> 5); } }
        
        /// <summary> X block coordinate of this position. </summary>
        public int BlockX { get { return X >> 5; } }
        
        /// <summary> T block coordinate of this position. </summary>
        public int BlockY { get { return Y >> 5; } }
        
        /// <summary> Z block coordinate of this position. </summary>
        public int BlockZ { get { return Z >> 5; } }       
        
        
        public override bool Equals(object obj) { return (obj is Position) && Equals((Position)obj); }
        
        public bool Equals(Position other) {
            return X == other.X && Y == other.Y && Z == other.Z;
        }
        
        public override int GetHashCode() {
            return 1000000007 * X + 1000000009 * Y + 1000000021 * Z;
        }
        
        public static bool operator == (Position a, Position b) { return a.Equals(b); }
        
        public static bool operator != (Position a, Position b) { return !a.Equals(b); }
    }
    
    
    /// <summary> Represents orientation / rotation of an entity. </summary>
    public struct Orientation {
        
        /// <summary> Rotation around X axis in packed form. </summary>
        public byte RotX;
        
        /// <summary> Rotation around Y axis in packed form. (yaw) </summary>
        public byte RotY;
        
        /// <summary> Rotation around Z axis in packed form. </summary>
        public byte RotZ;
        
        /// <summary> Rotation of head around X axis in packed form. (pitch) </summary>
        public byte HeadX;
        
        
        public Orientation(byte yaw, byte pitch) { RotX = 0; RotY = yaw; RotZ = 0; HeadX = pitch; }        
        
        /// <summary> Converts angle in range [0, 256) into range [0, 360). </summary>
        public static short PackedToDegrees(byte packed) {
            return (short)(packed * 360 / 256);
        }
        
        /// <summary> Converts angle in degrees into range [0, 256) </summary>
        public static byte DegreesToPacked(short degrees) {
            return (byte)(degrees * 256 / 360);
        }
    }
}
