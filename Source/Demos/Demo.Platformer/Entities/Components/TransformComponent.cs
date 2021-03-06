﻿using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Entities;

namespace Demo.Platformer.Entities.Components
{
    [EntityComponent]
    public class TransformComponent : PoolableComponent
    {
        private TransformFlags _flags = TransformFlags.All; // dirty flags, set all dirty flags when created
        private Matrix2 _localMatrix; // model space to local space
        private TransformComponent _parent; // parent
        private Matrix2 _worldMatrix; // local space to world space
        private Vector2 _position;
        private float _rotation;
        private Vector2 _scale = Vector2.One;

        public Vector2 WorldPosition => WorldMatrix.Translation;
        public Vector2 WorldScale => WorldMatrix.Scale;
        public float WorldRotation => WorldMatrix.Rotation;

        public Vector2 Position
        {
            get { return _position; }
            set
            {
                _position = value;
                LocalMatrixBecameDirty();
                WorldMatrixBecameDirty();
            }
        }

        public float Rotation
        {
            get { return _rotation; }
            set
            {
                _rotation = value;
                LocalMatrixBecameDirty();
                WorldMatrixBecameDirty();
            }
        }

        public Vector2 Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                LocalMatrixBecameDirty();
                WorldMatrixBecameDirty();
            }
        }

        public Matrix2 LocalMatrix
        {
            get
            {
                RecalculateLocalMatrixIfNecessary();
                return _localMatrix;
            }
        }

        public Matrix2 WorldMatrix
        {
            get
            {
                RecalculateWorldMatrixIfNecessary();
                return _worldMatrix;
            }
        }

        public TransformComponent Parent
        {
            get { return _parent; }
            set
            {
                if (_parent == value)
                    return;

                var oldParentTransform = Parent;
                _parent = value;
                OnParentChanged(oldParentTransform, value);
            }
        }

        internal event Action TransformBecameDirty;

        public override void Reset()
        {
            Parent = null;
            _flags = TransformFlags.All;
            _localMatrix = Matrix2.Identity;
            _worldMatrix = Matrix2.Identity;
            _position = Vector2.Zero;
            _rotation = 0;
            _scale = Vector2.One;
        }

        public void GetLocalMatrix(out Matrix2 matrix)
        {
            RecalculateLocalMatrixIfNecessary();
            matrix = _localMatrix;
        }

        public void GetWorldMatrix(out Matrix2 matrix)
        {
            RecalculateWorldMatrixIfNecessary();
            matrix = _worldMatrix;
        }

        protected internal void LocalMatrixBecameDirty()
        {
            _flags |= TransformFlags.LocalMatrixIsDirty;
        }

        protected internal void WorldMatrixBecameDirty()
        {
            _flags |= TransformFlags.WorldMatrixIsDirty;
            TransformBecameDirty?.Invoke();
        }

        private void OnParentChanged(TransformComponent oldParent, TransformComponent newParent)
        {
            var parent = oldParent;
            while (parent != null)
            {
                parent.TransformBecameDirty -= ParentOnTransformBecameDirty;
                parent = parent.Parent;
            }

            parent = newParent;
            while (parent != null)
            {
                parent.TransformBecameDirty += ParentOnTransformBecameDirty;
                parent = parent.Parent;
            }
        }

        private void ParentOnTransformBecameDirty()
        {
            _flags |= TransformFlags.All;
        }

        private void RecalculateWorldMatrixIfNecessary()
        {
            if ((_flags & TransformFlags.WorldMatrixIsDirty) == 0)
                return;

            RecalculateLocalMatrixIfNecessary();
            RecalculateWorldMatrix(ref _localMatrix, out _worldMatrix);
            _flags &= ~TransformFlags.WorldMatrixIsDirty;
        }

        private void RecalculateLocalMatrixIfNecessary()
        {
            if ((_flags & TransformFlags.LocalMatrixIsDirty) == 0)
                return;

            RecalculateLocalMatrix(out _localMatrix);

            _flags &= ~TransformFlags.LocalMatrixIsDirty;
            WorldMatrixBecameDirty();
        }

        private void RecalculateWorldMatrix(ref Matrix2 localMatrix, out Matrix2 matrix)
        {
            if (Parent != null)
            {
                Parent.GetWorldMatrix(out matrix);
                Matrix2.Multiply(ref matrix, ref localMatrix, out matrix);
            }
            else
            {
                matrix = localMatrix;
            }
        }

        private void RecalculateLocalMatrix(out Matrix2 matrix)
        {
            if (Parent != null)
            {
                var parentPosition = Parent.Position;
                matrix = Matrix2.CreateTranslation(-parentPosition) * Matrix2.CreateScale(_scale) *
                         Matrix2.CreateRotationZ(-_rotation) * Matrix2.CreateTranslation(parentPosition) *
                         Matrix2.CreateTranslation(_position);
            }
            else
            {
                matrix = Matrix2.CreateScale(_scale) * Matrix2.CreateRotationZ(-_rotation) *
                         Matrix2.CreateTranslation(_position);
            }
        }

        [Flags]
        private enum TransformFlags : byte
        {
            WorldMatrixIsDirty = 1 << 0,
            LocalMatrixIsDirty = 1 << 1,
            All = WorldMatrixIsDirty | LocalMatrixIsDirty
        }
    }
}
