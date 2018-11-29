﻿
using UnityEngine;

namespace U.movin
{
    public class BodyLayer
    {
        public GameObject gameObject;
        public Transform transform
        {
            get { return gameObject.transform; }
        }

        public Movin body;
        public BodymovinLayer content;
        public BodyShape[] shapes;

        public MotionProps mpos;
        public MotionProps mscale;
        public MotionProps mrotx;
        public MotionProps mroty;
        public MotionProps mrotz;

        public Vector3 positionOffset;
        public Vector3 finalRotation = Vector3.zero;

        public bool positionAnimated = false;
        public bool scaleAnimated = false;
        public bool rotationXAnimated = false;
        public bool rotationYAnimated = false;
        public bool rotationZAnimated = false;


        public BodyLayer(Movin body, BodymovinLayer layer)
        {
            this.body = body;
            this.content = layer;

            gameObject = new GameObject(content.ind + "  " + content.nm);
            transform.SetParent(body.transform, false);

            positionOffset = new Vector3(body.content.w / 2, -(body.content.h / 2), 0);

            transform.localPosition = content.position - positionOffset;
            transform.localRotation = content.rotation;
            transform.localScale = content.scale;

            finalRotation = content.rotationEuler;


            /* POSITION ANIM SETUP */

            positionAnimated = content.positionSets.Length > 0;
            if (positionAnimated)
            {
                mpos = new MotionProps
                {
                    keys = content.positionSets.Length
                };

                SetKeyframe(ref mpos, content.positionSets, 0);
                //Debug.Log("----- start: " + content.positionSets[0].s + "    end: " + content.positionSets[0].e + "    p: " + mpos.percent);
            }


            /* SCALE ANIM SETUP */

            scaleAnimated = content.scaleSets.Length > 0;
            if (scaleAnimated)
            {
                mscale = new MotionProps
                {
                    keys = content.scaleSets.Length
                };

                SetKeyframe(ref mscale, content.scaleSets, 0);
            }


            /* ROTATION ANIM SETUP */

            rotationXAnimated = content.rotationXSets.Length > 0;
            if (rotationXAnimated)
            {
                mrotx = new MotionProps
                {
                    keys = content.rotationXSets.Length
                };

                SetKeyframe(ref mrotx, content.rotationXSets, 0);
            }

            rotationYAnimated = content.rotationYSets.Length > 0;
            if (rotationYAnimated)
            {
                mroty = new MotionProps
                {
                    keys = content.rotationYSets.Length
                };

                SetKeyframe(ref mroty, content.rotationYSets, 0);
            }

            rotationZAnimated = content.rotationZSets.Length > 0;
            if (rotationZAnimated)
            {
                mrotz = new MotionProps
                {
                    keys = content.rotationZSets.Length
                };

                SetKeyframe(ref mrotz, content.rotationZSets, 0);
            }



            /* SHAPES */

            //Debug.Log("layer index:  " + content.ind + "   parent:  " + content.parent);

            shapes = new BodyShape[content.shapes.Length];

            int j = 0;
            for (int i = content.shapes.Length - 1; i >= 0; i--)
            {
                BodyShape shape = new BodyShape(this, content.shapes[i], 0.85f);
                shapes[i] = shape;

                //if (layer.ind == 8 && i == 1)
                //{
                //Debug.Log("bounds:  " + shape.filter.mesh.bounds);
                //shape.SetBounds(113.0f);
                //Debug.Log("recalc:  " + shape.filter.mesh.bounds);
                //}

                //shape.transform.localPosition += new Vector3(0, 0, -32 * j);
                j += 1;
            }
        }


        public void SetKeyframe(ref MotionProps prop, BodymovinAnimatedProperties[] set, int k = 0)
        {
            prop.completed = false;
            if (prop.keys <= 0) { return; }

            prop.key = k;
            prop.startFrame = set[k].t;
            prop.endFrame = set.Length > k ? set[k + 1].t : prop.startFrame;
            prop.currentOutTangent = set[k].o;
            prop.nextInTangent = set[k].i;

            //Debug.Log("key: " + k + "   out: " + set[k].o + "     nxt in: " + set[k].i);

        }


        public void Update(float frame)
        {

            // IN + OUT POINTS FOR LAYER

            if (!gameObject.activeInHierarchy && frame >= content.inFrame) { gameObject.SetActive(true); }
            if (!gameObject.activeInHierarchy) { return; }

            if (gameObject.activeInHierarchy && (frame >= content.outFrame || frame < content.inFrame))
            {
                gameObject.SetActive(false);
                return;
            }


            // SEND DOWN UPDATES

            foreach (BodyShape shape in shapes)
            {
                shape.Update(frame);
            }


            // ANIM PROPS

            if (positionAnimated && !mpos.completed)
            {
                UpdateProperty(frame, ref mpos, content.positionSets);
            }

            if (scaleAnimated && !mscale.completed)
            {
                UpdateProperty(frame, ref mscale, content.scaleSets);
            }

            if (rotationXAnimated && !mrotx.completed)
            {
                UpdateProperty(frame, ref mrotx, content.rotationXSets);
            }

            if (rotationYAnimated && !mroty.completed)
            {
                UpdateProperty(frame, ref mroty, content.rotationYSets);
            }

            if (rotationZAnimated && !mrotz.completed)
            {
                UpdateProperty(frame, ref mrotz, content.rotationZSets);
            }

            if (rotationXAnimated || rotationYAnimated || rotationZAnimated)
            {
                //Debug.Log("Rot - " + finalRotation);
                transform.localRotation = Quaternion.Euler(finalRotation);
            }
        }

        public void UpdateProperty(float frame, ref MotionProps m, BodymovinAnimatedProperties[] set)
        {

            if (m.keys <= 0)
            {
                Debug.Log("NO PROP KEYS TO ANIMATE!");
                m.completed = true;
                return;
            }

            if (frame >= m.endFrame)
            {
                if (m.key + 1 == set.Length - 1)
                {
                    m.completed = true;
                    //Debug.Log("****** Prop Animation done! ******");
                    return;
                }

                SetKeyframe(ref m, set, m.key + 1);
            }

            m.percent = (frame - m.startFrame) / (m.endFrame - m.startFrame);


            /* ----- CUBIC BEZIER ----- */
            float ease = Ease.CubicBezier(Vector2.zero, m.currentOutTangent, m.nextInTangent, Vector2.one, m.percent);

            if (set == content.positionSets)
            {
                if (m.percent < 0)
                {

                    transform.localPosition = set[m.key].s - positionOffset;
                    return;
                }

                transform.localPosition = set[m.key].s + ((set[m.key].e - set[m.key].s) * ease) - positionOffset;

            }
            else if (set == content.scaleSets)
            {
                if (m.percent < 0)
                {
                    transform.localScale = set[m.key].s;
                    return;
                }

                transform.localScale = set[m.key].s + ((set[m.key].e - set[m.key].s) * ease);

            }
            else if (set == content.rotationXSets)
            {
                if (m.percent < 0)
                {
                    finalRotation.x = set[m.key].sf;
                    return;
                }

                finalRotation.x = set[m.key].sf + ((set[m.key].ef - set[m.key].sf) * ease);

            }
            else if (set == content.rotationYSets)
            {
                if (m.percent < 0)
                {
                    finalRotation.y = set[m.key].sf;
                    return;
                }

                finalRotation.y = set[m.key].sf + ((set[m.key].ef - set[m.key].sf) * ease);

            }
            else if (set == content.rotationZSets)
            {
                if (m.percent < 0)
                {
                    finalRotation.z = set[m.key].sf;
                    return;
                }

                finalRotation.z = set[m.key].sf + ((set[m.key].ef - set[m.key].sf) * ease);

            }

        }




        public void ResetKeyframes()
        {
            if (positionAnimated) { SetKeyframe(ref mpos, content.positionSets, 0); }
            if (scaleAnimated) { SetKeyframe(ref mscale, content.scaleSets, 0); }
            if (rotationXAnimated) { SetKeyframe(ref mrotx, content.rotationXSets, 0); }
            if (rotationYAnimated) { SetKeyframe(ref mroty, content.rotationYSets, 0); }
            if (rotationZAnimated) { SetKeyframe(ref mrotz, content.rotationZSets, 0); }

            foreach (BodyShape shape in shapes)
            {
                if (shape.animated)
                {
                    shape.SetKeyframe(0);
                }
            }

        }
    }
}