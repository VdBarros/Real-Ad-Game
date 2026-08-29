using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class FightBoard : MonoBehaviour
    {
        EnemyFigure[] byNode;

        PlayerFigure player;

        EnemyFigure joined;

        int joinedNodeId = TapAim.Nothing;

        WorldPoint axis;

        WorldPoint playerPost;

        WorldPoint enemyPost;

        public bool IsTurning
        {
            get
            {
                if (byNode == null)
                {
                    return false;
                }

                foreach (var enemy in byNode)
                {
                    if (enemy != null && enemy.IsTurning)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal void Begin(int nodeCount, PlayerFigure figure, IReadOnlyList<EnemyFigure> enemies)
        {
            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            byNode = new EnemyFigure[nodeCount];
            player = figure;
            joined = null;
            joinedNodeId = TapAim.Nothing;

            foreach (var enemy in enemies)
            {
                byNode[enemy.NodeId] = enemy;
            }
        }

        public EnemyFigure Of(int nodeId)
        {
            RequireABeginning();
            return nodeId >= 0 && nodeId < byNode.Length ? byNode[nodeId] : null;
        }

        public WorldPoint SiteOf(int nodeId)
        {
            var enemy = Of(nodeId);
            if (enemy == null)
            {
                return default(WorldPoint);
            }

            var ground = enemy.Ground;
            return new WorldPoint(ground.X, ground.Y + OrbStream.Lift, ground.Z);
        }

        public void Turn(float deltaSeconds)
        {
            if (byNode == null)
            {
                return;
            }

            foreach (var enemy in byNode)
            {
                if (enemy != null)
                {
                    enemy.Turn(deltaSeconds);
                }
            }
        }

        public void Square(int nodeId, WorldPoint approach)
        {
            RequireABeginning();

            var enemy = Of(nodeId);
            if (player == null || enemy == null)
            {
                return;
            }

            var apart = FigureFacing.Between(player.Ground, enemy.Ground);
            var toward = FigureFacing.IsAimed(apart) ? apart : approach;

            if (!FigureFacing.IsAimed(toward))
            {
                return;
            }

            player.Face(toward);
            enemy.Face(FigureFacing.Reversed(toward));
        }

        public void Show(Journey journey)
        {
            if (journey == null)
            {
                throw new ArgumentNullException(nameof(journey));
            }

            RequireABeginning();

            var fight = journey.Fight;
            if (!fight.IsJoined)
            {
                Leave();
                return;
            }

            if (journey.Walk.ArrivedNodeId != joinedNodeId)
            {
                Join(journey.Walk.ArrivedNodeId, journey.Walk.Facing, fight.Dissolves);
            }

            Play(fight);
        }

        void Join(int nodeId, WorldPoint facing, bool doomed)
        {
            joinedNodeId = nodeId;
            joined = Of(nodeId);
            axis = facing;
            playerPost = player != null ? player.Ground : default(WorldPoint);
            enemyPost = joined != null ? joined.Ground : playerPost;

            Square(nodeId, axis);

            if (doomed && joined != null)
            {
                joined.Doom();
            }
        }

        void Play(Fight fight)
        {
            if (player != null)
            {
                player.StandOn(Along(playerPost, fight.Shove));
            }

            if (joined == null || joined.HasFallen)
            {
                return;
            }

            joined.StandOn(Along(enemyPost, fight.Recoil));
            joined.Answer(FigureCues.Answering(fight));
            joined.Dissolve(fight.Fade);
        }

        void Leave()
        {
            joined = null;
            joinedNodeId = TapAim.Nothing;
        }

        WorldPoint Along(WorldPoint post, float distance)
        {
            return new WorldPoint(
                post.X + axis.X * distance, post.Y + axis.Y * distance, post.Z + axis.Z * distance);
        }

        void RequireABeginning()
        {
            if (byNode == null)
            {
                throw new InvalidOperationException(
                    "The board fights the enemies of a level it has not been given. Call Begin.");
            }
        }
    }
}
