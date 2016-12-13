/* Copyright © 2016 Jonathan Tiefer - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the GNU Lesser General Public License (LGPL)
 *
 * You should have received a copy of the LGPL license with
 * this file.
 *
 * /

/*  This file is part of Tiferix.DotCoolGridView control library.
*
*   Tiferix.DotCoolGridView is free software: you can redistribute it and/or modify
*   it under the terms of the GNU Lesser General Public License as published by
*   the Free Software Foundation, either version 3 of the License, or
*    (at your option) any later version.
*
*   Tiferix.DotCoolGridView is distributed in the hope that it will be useful,
*   but WITHOUT ANY WARRANTY; without even the implied warranty of
*   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*   GNU Lesser General Public License for more details.
*
*  You should have received a copy of the GNU Lesser General Public License
*   along with Tiferix.DotCoolGridView.  If not, see <http://www.gnu.org/licenses/>.
*/


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tiferix.Global;
using DotCoolControls.Tools;
using DotCoolControls.VisualSettings;

namespace Tiferix.DotCoolGrid
{
    /// <summary>
    /// An expansion of the native .NET DataGridView control which has added stunning visual effects.  The DotCoolGridView has effects such as adjustable 
    /// gradient column headers and background.  In addition, both the cells and column headers can utilize alpha-blending and display translucent color 
    /// cells over the background.  Some other significant features include background images that can be merged both with gradient backgrounds and 
    /// displayed behind alpha-blended cells, columns and rows.
    /// </summary>
    public class DotCoolGridView : DataGridView
    {
        #region Member Variables

        private int originalExStyle = -1;
        private bool enableFormLevelDoubleBuffering = true;

        private int m_iPaintErrCount = 0;

        private bool m_blCancelPaint = false;

        #endregion

        #region Member Object Variables

        private Dictionary<string, object> m_dicSetCellTransOrigVals = new Dictionary<string, object>();

        #endregion

        #region Member Data Object Variables
        #endregion

        #region Cell Style Variables

        private DataGridViewCellStyle m_DefaultCellStyle = null;

        #endregion

        #region Background Gradient Variables

        /// <summary>
        /// Contains the various gradient settings for the background of the DotCoolGridView class.
        /// </summary>
        protected VisualSettingProperties<GradientVisualSettings> m_GradBackSettings = null;

        #endregion

        #region Background Image Variables

        /// <summary>
        /// Contains the various image settings for the background of the DotCoolGridView class.
        /// </summary>
        protected VisualSettingProperties<ImageVisualSettings> m_ImageBackSettings = null;

        #endregion

        #region Column Header Gradient Variables

        /// <summary>
        /// Contains the various gradient settings for the column headers of the DotCoolGridView class.
        /// </summary>
        protected VisualSettingProperties<GradientVisualSettings> m_GradColHdrSettings = null;

        #endregion

        #region Column Header Border Variables                            

        /// <summary>
        /// Contains the various border settings for the column headers of the DotCoolGridView class.
        /// </summary>
        protected VisualSettingProperties<BorderVisualSettings> m_BorderColHdrSettings = null;

        #endregion

        #region Column Header Translucency Variables

        private bool m_blDrawColHdrTransColor = false;

        private Color m_ColHdrTransColor = SystemColors.Control;

        private int m_iColHdrTransAlpha = 0;

        #endregion

        #region Cell Translucency Variables

        private bool m_blDrawCellTransColor = false;

        private Color m_CellTransColor = SystemColors.Control;

        private int m_iCellTransAlpha = 0;

        #endregion

        #region Selected Cell Translucency Variables

        private bool m_blDrawCellSelectTransColor = false;

        private Color m_CellSelectTransColor = SystemColors.Control;

        private int m_iCellSelectTransAlpha = 0;

        #endregion

        #region Column Style Variables

        private Font m_ColHdrFont = new Font("Arial", 9f, FontStyle.Regular);

        #endregion        

        #region Constructor/Initialization

        /// <summary>
        /// Constructor
        /// </summary>
        public DotCoolGridView()
            : base()
        {
            try
            {
                this.SetStyle(ControlStyles.DoubleBuffer, true);
                this.SetStyle(ControlStyles.ResizeRedraw, true);
                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                this.SetStyle(ControlStyles.UserPaint, true);
                this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);


                this.DoubleBuffered = true;

                this.EnableHeadersVisualStyles = false;

                m_GradBackSettings = VisualSettingPropGenerator.CreateGradientVisualSettings();
                m_ImageBackSettings = VisualSettingPropGenerator.CreateImageVisualSettings();
                m_GradColHdrSettings = VisualSettingPropGenerator.CreateGradientVisualSettings();
                m_BorderColHdrSettings = VisualSettingPropGenerator.CreateBorderVisualSettings();

                m_GradBackSettings[VisualSettingEnum.Normal].DrawGradient = false;
                m_GradBackSettings[VisualSettingEnum.Normal].GradientColor1 = SystemColors.Control;
                m_GradBackSettings[VisualSettingEnum.Normal].GradientColor2 = SystemColors.Control;
                m_GradBackSettings[VisualSettingEnum.Normal].GradientType = CoolGradientType.Horizontal;

                m_ImageBackSettings[VisualSettingEnum.Normal].EnableImage = false;
                m_ImageBackSettings[VisualSettingEnum.Normal].Transparent = false;

                m_GradColHdrSettings[VisualSettingEnum.Normal].DrawGradient = false;
                m_GradColHdrSettings[VisualSettingEnum.Normal].GradientColor1 = SystemColors.Control;
                m_GradColHdrSettings[VisualSettingEnum.Normal].GradientColor2 = SystemColors.Control;
                m_GradColHdrSettings[VisualSettingEnum.Normal].GradientType = CoolGradientType.Horizontal;

                m_BorderColHdrSettings[VisualSettingEnum.Normal].BorderColor = Color.Black;
                m_BorderColHdrSettings[VisualSettingEnum.Normal].BorderWidth = 1;

                m_DefaultCellStyle = new DataGridViewCellStyle(base.DefaultCellStyle);
            }
            catch (Exception err)
            {
                ErrorHandler.ShowErrorMessage(err, "Error in Constructor function of DotCoolGridView class.");
            }

        }

        /// <summary>
        /// Encapsulates the information needed when creating a control.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                if (originalExStyle == -1)
                    originalExStyle = base.CreateParams.ExStyle;

                CreateParams cp = base.CreateParams;
                if (enableFormLevelDoubleBuffering)
                    cp.ExStyle |= 0x02000000;   // WS_EX_COMPOSITED
                else
                    cp.ExStyle = originalExStyle;

                return cp;
            }
        }

        #endregion

        #region Paint/GDI+ Functions, Event Handlers

        /// <summary>        
        /// Paints the background of the System.Windows.Forms.DataGridView.
        /// This function is overridden in CoolGrid to allow for the custom gradient painting of the background.                
        /// </summary>
        /// <param name="graphics">The System.Drawing.Graphics used to paint the background.</param>
        /// <param name="clipBounds">A System.Drawing.Rectangle that represents the area of the System.Windows.Forms.DataGridView
        /// that needs to be painted.</param>
        /// <param name="gridBounds">A System.Drawing.Rectangle that represents the area in which cells are drawn.</param>
        protected override void PaintBackground(System.Drawing.Graphics graphics, System.Drawing.Rectangle clipBounds, System.Drawing.Rectangle gridBounds)
        {
            if (m_blCancelPaint)
                return;

            base.PaintBackground(graphics, clipBounds, gridBounds);

            try
            {                
                float fGradSpan = 0f;

                if (UseDefaultBackGradientSpan)
                    fGradSpan = CoolGradient.GetDefaultGradientSpan(BackGradientType);
                else
                    fGradSpan = BackGradientSpan;

                if (DrawBackgroundGradient)
                    CoolGradient.DrawGradient(BackGradientType, graphics, BackgroundGradientColor1, BackgroundGradientColor2, gridBounds,
                                                            fGradSpan, BackGradientOffset.X, BackGradientOffset.Y);

                if (DrawBackgroundImage && BackgroundImage != null)
                {
                    if (BackgroundImageSizeMode == CoolImageSizeMode.Normal)
                        CoolDraw.DrawImage(BackgroundImage, graphics, gridBounds, BackgroundImageAlign, BackgroundImageOffset,
                                                        BackgroundImageTransparent, BackgroundImageTransColor);
                    else
                        CoolDraw.DrawImage(BackgroundImage, graphics, gridBounds, BackgroundImageSizeMode, BackgroundImageTransparent,
                                                        BackgroundImageTransColor);
                }//end if
            }
            catch (Exception err)
            {
                ErrorHandler.ShowErrorMessage(err, "Error in PaintBackground function of DotCoolGridView control.", "", m_iPaintErrCount > 4 ? true : false);
                m_iPaintErrCount++;
            }
        }

        /// <summary>                
        ///    Raises the System.Windows.Forms.DataGridView.CellPainting event.                
        ///    This function is overridden in CoolGrid to allow for the painting of gradient column headers and translucent headers and cells.
        /// 
        ///    Exceptions:
        ///   T:System.ArgumentOutOfRangeException:
        ///     The value of the System.Windows.Forms.DataGridViewCellPaintingEventArgs.ColumnIndex
        ///     property of e is greater than the number of columns in the control minus one.-or-The
        ///     value of the System.Windows.Forms.DataGridViewCellPaintingEventArgs.RowIndex
        ///     property of e is greater than the number of rows in the control minus one.
        /// </summary>
        /// <param name="e">A System.Windows.Forms.DataGridViewCellPaintingEventArgs that contains the event data.</param>        
        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {            
            if (m_blCancelPaint)
                return;

            base.OnCellPainting(e);

            Brush semiTransBrush = null;
            Pen penBorder = null;
            Brush textBrush = null;

            try
            {
                if (!DrawColHeaderGradient && !DrawColHeaderTransColor && e.RowIndex == -1)
                    return;
                else if (!DrawCellTransColor && !DrawSelectedCellTransColor && e.RowIndex >= 0)
                    return;

                Rectangle newRect = new Rectangle(e.CellBounds.X + 1,
                                                                   e.CellBounds.Y + 1, e.CellBounds.Width - 4,
                                                                   e.CellBounds.Height - 4);

                Rectangle BorderRect = new Rectangle(e.CellBounds.X,
                                                                   e.CellBounds.Y,
                                                                   e.CellBounds.Width - 1,
                                                                   e.CellBounds.Height - 1);

                textBrush = new SolidBrush(e.CellStyle.ForeColor);

                bool blCellSelected = false;

                if (e.RowIndex == -1)
                {
                    if (!DrawColHeaderGradient && !DrawColHeaderTransColor)
                        return;

                    if (DrawColHeaderGradient)
                    {
                        penBorder = new Pen(
                                                            Color.FromArgb(100,
                                                                ColHeaderGradientBorderColor.R, ColHeaderGradientBorderColor.G,
                                                                ColHeaderGradientBorderColor.B), ColHeaderGradientBorderWidth);

                        Rectangle rectColHeaderRect = Rectangle.Empty;

                        rectColHeaderRect = new Rectangle(e.CellBounds.X - 1,
                                                                            e.CellBounds.Y - 0,
                                                                            e.CellBounds.Width - 1,
                                                                            e.CellBounds.Height - 1);

                        float fGradSpan = 0f;

                        if (UseDefaultColHeaderGradientSpan)
                            fGradSpan = CoolGradient.GetDefaultGradientSpan(ColHeaderGradientType);
                        else
                            fGradSpan = ColHeaderGradientSpan;

                        //CoolGradient.DrawGradient(ColHeaderGradientType, e.Graphics, ColHeaderGradientColor1, ColHeaderGradientColor2, newRect);                        
                        CoolDraw.DrawGradientRectangle(ColHeaderGradientType, e.Graphics, ColHeaderGradientColor1, ColHeaderGradientColor2,
                                                                         rectColHeaderRect, ColHeaderGradientBorderColor, ColHeaderGradientBorderWidth, fGradSpan,
                                                                         ColHeaderGradientOffset.X, ColHeaderGradientOffset.Y);
                    }
                    else if (DrawColHeaderTransColor)
                    {
                        semiTransBrush = new SolidBrush(
                                                            Color.FromArgb(ColHeaderTransAlpha, ColHeaderTransColor.R,
                                                                                    ColHeaderTransColor.G, ColHeaderTransColor.B));

                        penBorder = new Pen(GridColor, 1);

                        e.Graphics.FillRectangle(semiTransBrush, BorderRect);
                    }//end if                                        
                }
                else
                {
                    if ((e.ColumnIndex >= 0 && !this[e.ColumnIndex, e.RowIndex].Selected) || e.ColumnIndex == -1)
                    {
                        if (!DrawCellTransColor)
                            return;

                        semiTransBrush = new SolidBrush(
                                                                Color.FromArgb(CellTransAlpha, CellTransColor.R,
                                                                                        CellTransColor.G, CellTransColor.B));

                        penBorder = new Pen(GridColor, 1);

                        e.Graphics.FillRectangle(semiTransBrush, BorderRect);
                    }
                    else
                    {
                        if (!m_blDrawCellSelectTransColor)
                            return;

                        blCellSelected = true;

                        penBorder = new Pen(GridColor, 1);

                        semiTransBrush = new SolidBrush(
                                                                Color.FromArgb(SelectedCellTransAlpha, SelectedCellTransColor.R,
                                                                                        SelectedCellTransColor.G, SelectedCellTransColor.B));

                        e.Graphics.FillRectangle(semiTransBrush, BorderRect);
                    }//end if
                }//end if

                // Draw the grid lines (only the right and bottom lines;
                // DataGridView takes care of the others).
                e.Graphics.DrawLine(penBorder, e.CellBounds.Left,
                    e.CellBounds.Bottom - 1, e.CellBounds.Right - 1,
                    e.CellBounds.Bottom - 1);
                e.Graphics.DrawLine(penBorder, e.CellBounds.Right - 1,
                    e.CellBounds.Top, e.CellBounds.Right - 1,
                    e.CellBounds.Bottom - 1);

                // Draw the text content of the cell, ignoring alignment.
                bool blHasData = false;

                if (e.Value != null)
                {
                    if (Convert.ToString(e.Value) != "")
                        blHasData = true;
                    else
                        blHasData = false;
                }//end if                    

                if (e.RowIndex == -1)
                {
                    if (this.SortedColumn != null)
                    {
                        if (this.SortedColumn.Index == e.ColumnIndex && SortOrder != SortOrder.None)
                            DrawColHeaderSortGlyph(e, this.SortOrder);
                    }//end if
                }//end if

                if (blHasData)
                {
                    if (e.RowIndex == -1)
                    {
                        CoolDraw.DrawText(e.Value.ToString(), e.Graphics, newRect.Location, newRect.Size, e.RowIndex == -1 ? ColHeadersFont : e.CellStyle.Font,
                                                    e.CellStyle.ForeColor, (ContentAlignment)e.CellStyle.Alignment, false);
                    }
                    else
                    {
                        if (e.ColumnIndex >= 0)
                        {
                            CoolDraw.DrawText(e.Value.ToString(), e.Graphics, newRect.Location, newRect.Size, e.CellStyle.Font,
                                                        !blCellSelected ? e.CellStyle.ForeColor : e.CellStyle.SelectionForeColor,
                                                        (ContentAlignment)e.CellStyle.Alignment, false);
                        }//end if
                    }//end if    
                }//end if

                //if (e.RowIndex == -1)
                e.Handled = true;
            }
            catch (Exception err)
            {
                ErrorHandler.ShowErrorMessage(err, "Error in OnCellPainting function of DotCoolGridView control.", "", m_iPaintErrCount > 4 ? true : false);
                m_iPaintErrCount++;
            }
            finally
            {
                if (semiTransBrush != null)
                    semiTransBrush.Dispose();

                if (penBorder != null)
                    penBorder.Dispose();

                if (textBrush != null)
                    textBrush.Dispose();
            }
        }

        /// <summary>
        /// Draws the sort indicator glyph graphic in the sorted column header of the grid when custom drawing of column headers is 
        /// being used in the DotCoolGridView control.   The sort indicator will be custom drawn depending on the sort order set in the 
        /// column, using a triangle that is filled with a linear gradient brush.
        /// </summary>
        protected virtual void DrawColHeaderSortGlyph(DataGridViewCellPaintingEventArgs e, SortOrder direction)
        {
            Bitmap bmpSortGlyph = null;
            Graphics gSortGlyph = null;
            Pen penSortGlyphBorder = null;
            LinearGradientBrush linGradBrush = null;

            try
            {
                Point[] points = null;

                if (direction == SortOrder.Ascending)
                {
                    points = new Point[] { new Point(5, 0), new Point(10, 10), new Point(0, 10) };
                }
                else
                {
                    points = new Point[] { new Point(5, 10), new Point(0, 0), new Point(10, 0) };
                }//end if

                Rectangle rectSortGlyph = new Rectangle(0, 0, 11, 11);
                bmpSortGlyph = new Bitmap(rectSortGlyph.Width, rectSortGlyph.Height);
                gSortGlyph = Graphics.FromImage(bmpSortGlyph);
                gSortGlyph.SmoothingMode = SmoothingMode.HighQuality;

                Rectangle rectSortGlyphGrad = new Rectangle(0, 0, 9, 9);
                linGradBrush = new LinearGradientBrush(
                                                rectSortGlyphGrad, Color.LightGray, Color.Gray,
                                                (direction == SortOrder.Ascending) ?
                                                            LinearGradientMode.ForwardDiagonal : LinearGradientMode.BackwardDiagonal);

                gSortGlyph.FillPolygon(linGradBrush, points);

                penSortGlyphBorder = new Pen(Color.Black, 1);
                gSortGlyph.DrawPolygon(penSortGlyphBorder, points);

                Rectangle rectSortGlyphDest =
                    new Rectangle(e.CellBounds.Left + e.CellBounds.Width - bmpSortGlyph.Width - 8,
                                          Convert.ToInt32(((e.CellBounds.Top + e.CellBounds.Height) - bmpSortGlyph.Height) / 2),
                                          11, 11);

                e.Graphics.DrawImage(bmpSortGlyph, rectSortGlyphDest.Left, rectSortGlyphDest.Top);
            }
            catch (Exception err)
            {
                ErrorHandler.ShowErrorMessage(err, "Error in DrawColHeaderSortGlyph function of DotCoolGridView control.");
            }
            finally
            {
                if (gSortGlyph != null)
                    gSortGlyph.Dispose();

                if (bmpSortGlyph != null)
                    bmpSortGlyph.Dispose();

                if (penSortGlyphBorder != null)
                    penSortGlyphBorder.Dispose();

                if (linGradBrush != null)
                    linGradBrush.Dispose();
            }
        }

        #endregion

        #region Cell Style Properties, Functions, Event Handlers

        /* NOT USED: Base Class CellStyle settings will work without any modification in the inherited class.
        public new DataGridViewCellStyle DefaultCellStyle
        {
            get
            {
                return m_DefaultCellStyle;
            }
            set
            {
                m_DefaultCellStyle = value;

                base.DefaultCellStyle = new DataGridViewCellStyle(m_DefaultCellStyle);

                if (DrawCellTransColor)
                    base.DefaultCellStyle.BackColor = Color.Transparent;                                
            }
        }

        protected override void OnCellStyleChanged(DataGridViewCellEventArgs e)
        {
            base.OnCellStyleChanged(e);            
        }
        */
        #endregion

        #region Column Header Style Properties, Functions, Event Handlers

        /// <summary>
        /// 
        /// </summary>
        public new DataGridViewCellStyle ColumnHeadersDefaultCellStyle
        {
            get
            {
                return base.ColumnHeadersDefaultCellStyle;
            }
            set
            {
                base.ColumnHeadersDefaultCellStyle = value;
                ColHeadersFont = base.ColumnHeadersDefaultCellStyle.Font;
            }
        }

        #endregion

        #region Cell Transparency/Translucency Properties, Functions   

        #endregion

        #region Background Color/Gradient Properties, Functions

        /// <summary>
        /// Indicates if the background will be drawn with a gradient, using the background gradient settings set in the control.
        /// </summary>
        [Browsable(true), DefaultValue(false), Category("CoolBackground"),
        Description("Indicates if the background will be drawn with a gradient, using the background gradient settings set in the control.")]
        public bool DrawBackgroundGradient
        {
            get
            {
                return m_GradBackSettings[VisualSettingEnum.Normal].DrawGradient;
            }
            set
            {
                m_GradBackSettings[VisualSettingEnum.Normal].DrawGradient = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// The starting color in the gradient path to be used for drawing the DotCoolGridView's background gradient.  
        /// </summary>        
        [Browsable(true), Category("CoolBackground"),
         Description("The starting color in the gradient path to be used for drawing the DotCoolGridView's background gradient.")]
        public virtual Color BackgroundGradientColor1
        {
            get
            {
                return m_GradBackSettings[VisualSettingEnum.Normal].GradientColor1;
            }
            set
            {
                m_GradBackSettings[VisualSettingEnum.Normal].GradientColor1 = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// The ending color in the gradient path to be used for drawing the DotCoolGridView's background gradient.  
        /// </summary>
        [Browsable(true), Category("CoolBackground"),
          Description("The ending color in the gradient path to be used for drawing the DotCoolGridView's background gradient.")]
        public virtual Color BackgroundGradientColor2
        {
            get
            {
                return m_GradBackSettings[VisualSettingEnum.Normal].GradientColor2;
            }
            set
            {
                m_GradBackSettings[VisualSettingEnum.Normal].GradientColor2 = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// Indicates the style/pattern to be used for drawing the DotCoolGridView's gradient background.
        /// </summary>
        [Browsable(true), DefaultValue(CoolGradientType.Horizontal), Category("CoolBackground"),
         Description("Indicates the style/pattern to be used for drawing the DotCoolGridView's gradient background.")]
        public virtual CoolGradientType BackGradientType
        {
            get
            {
                return m_GradBackSettings[VisualSettingEnum.Normal].GradientType;
            }
            set
            {
                m_GradBackSettings[VisualSettingEnum.Normal].GradientType = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// The span (size factor or expanse) of the gradient image to be used for drawing the DotCoolGridView's gradient background.
        /// Certain types of gradients will look more appealing in a control when drawn on a larger or smaller expanse.  Most gradients will look  
        /// ideal drawn with the default gradient span associated with the gradient type.
        /// </summary>        
        [Browsable(true), Category("CoolBackground"), DefaultValue(1f),
         Description("The span (size factor or expanse) of the gradient image to be used for drawing the DotCoolGridView's gradient background.")]
        public virtual float BackGradientSpan
        {
            get
            {
                return m_GradBackSettings[VisualSettingEnum.Normal].GradientSpan;
            }
            set
            {
                m_GradBackSettings[VisualSettingEnum.Normal].GradientSpan = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// Indicates if the default gradient span (size factor or expanse) associated with the selected gradient type used for drawing the DotCoolGridView's 
        /// gradient background.  Every gradient type has a default gradient span setting that usually will look most ideal for
        /// the type of gradient being drawn.  When this flag is set to true, the gradient span setting cannot be modified in the control, as the default 
        /// value will be used.
        /// </summary>        
        [Browsable(true), Category("CoolBackground"), DefaultValue(true),
         Description("Indicates if the default gradient span (size factor or expanse) associated with the selected gradient type used for drawing the " +
                          "DotCoolGridView's gradient background.")]
        public virtual bool UseDefaultBackGradientSpan
        {
            get
            {
                return m_GradBackSettings[VisualSettingEnum.Normal].UseDefaultGradientSpan;
            }
            set
            {
                m_GradBackSettings[VisualSettingEnum.Normal].UseDefaultGradientSpan = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// The X and Y offset position of the gradient image to be used for drawing the gradient background of the DotCoolGridView control.  Adjusting 
        /// the offset position of the gradient alters the lighting and color range of the gradient image in the control.
        /// </summary>        
        [Browsable(true), Category("CoolBackground"),
         Description("The X and Y offset position of the gradient image to be used for drawing the gradient background of the DotCoolGridView control.")]
        public virtual Point BackGradientOffset
        {
            get
            {
                return m_GradBackSettings[VisualSettingEnum.Normal].GradientOffset;
            }
            set
            {
                m_GradBackSettings[VisualSettingEnum.Normal].GradientOffset = value;
                this.Refresh();
            }
        }

        #endregion

        #region Background Image Properties, Functions

        /// <summary>
        /// Indicates if a background image will be drawn for the control.
        /// </summary>
        [Browsable(true), Category("CoolBackground"), DefaultValue(false),
         Description("Gets or sets the image displayed in the control.")]
        public virtual bool DrawBackgroundImage
        {
            get
            {
                return m_ImageBackSettings[VisualSettingEnum.Normal].EnableImage;
            }
            set
            {
                m_ImageBackSettings[VisualSettingEnum.Normal].EnableImage = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the background image displayed in the control.
        /// </summary>
        [Browsable(true), Category("CoolBackground"),
         Description("Gets or sets the image displayed in the control.")]
        public virtual new Image BackgroundImage
        {
            get
            {
                return m_ImageBackSettings[VisualSettingEnum.Normal].Image;
            }
            set
            {
                m_ImageBackSettings[VisualSettingEnum.Normal].Image = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// Indicates how the background image is displayed in the control.
        /// </summary>
        [Browsable(true), Category("CoolBackground"),
         Description("Gets or sets the image displayed in the control.")]
        public virtual CoolImageSizeMode BackgroundImageSizeMode
        {
            get
            {
                return m_ImageBackSettings[VisualSettingEnum.Normal].SizeMode;
            }
            set
            {
                m_ImageBackSettings[VisualSettingEnum.Normal].SizeMode = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the alignment of the image in the control.  Only works with Normal sized images.
        /// </summary>
        [Browsable(true), Category("CoolBackground"),
         Description("Gets or sets the alignment of the image in the control.  Only works with Normal sized images.")]
        public virtual ContentAlignment BackgroundImageAlign
        {
            get
            {
                return m_ImageBackSettings[VisualSettingEnum.Normal].ImageAlign;
            }
            set
            {
                m_ImageBackSettings[VisualSettingEnum.Normal].ImageAlign = value;

                if (BackgroundImageSizeMode == CoolImageSizeMode.Normal)
                    this.Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the x and y offset position of the image in the control.  Only works with Normal sized images.
        /// </summary>
        [Browsable(true), Category("CoolBackground"),
         Description("Gets or sets the x and y offset position of the image in the control.  Only works with Normal sized images.")]
        public virtual Point BackgroundImageOffset
        {
            get
            {
                return m_ImageBackSettings[VisualSettingEnum.Normal].ImageOffset;
            }
            set
            {
                m_ImageBackSettings[VisualSettingEnum.Normal].ImageOffset = value;

                if (BackgroundImageSizeMode == CoolImageSizeMode.Normal)
                    this.Refresh();
            }
        }

        /// <summary>
        /// Indicates if the background image will be drawn with transparency or as an opaque image.
        /// </summary>
        [Browsable(true), Category("CoolBackground"),
         Description("Indicates if the background image will be drawn with transparency or as an opaque image.")]
        public virtual bool BackgroundImageTransparent
        {
            get
            {
                return m_ImageBackSettings[VisualSettingEnum.Normal].Transparent;
            }
            set
            {
                m_ImageBackSettings[VisualSettingEnum.Normal].Transparent = value;

                this.Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the color of the image to use for transparency in the control.
        /// </summary>
        [Browsable(true), Category("CoolBackground"),
         Description("Gets or sets the color of the image to use for transparency in the control.")]
        public virtual Color BackgroundImageTransColor
        {
            get
            {
                return m_ImageBackSettings[VisualSettingEnum.Normal].TransparentColor;
            }
            set
            {
                m_ImageBackSettings[VisualSettingEnum.Normal].TransparentColor = value;

                this.Refresh();
            }
        }

        #endregion

        #region Column Header Color/Gradient Properties, Functions

        /// <summary>
        /// Indicates if the column headers will be drawn with a gradient, using the column header gradient settings set in the control.
        /// </summary>
        [Browsable(true), DefaultValue(false), Category("CoolColHeader"),
            Description("Indicates if the column headers will be drawn with a gradient, using the column header gradient settings set in the control.")]
        public bool DrawColHeaderGradient
        {
            get
            {
                return m_GradColHdrSettings[VisualSettingEnum.Normal].DrawGradient;
            }
            set
            {
                m_GradColHdrSettings[VisualSettingEnum.Normal].DrawGradient = value;

                if (value && DrawColHeaderTransColor)
                    DrawColHeaderTransColor = false;

                this.Refresh();
            }
        }

        /// <summary>
        /// The starting color in the gradient path to be used for drawing the gradients of the column headers.
        /// </summary>        
        [Browsable(true), Category("CoolColHeader"),
            Description("The starting color in the gradient path to be used for drawing the gradients of the column headers.")]
        public virtual Color ColHeaderGradientColor1
        {
            get
            {
                return m_GradColHdrSettings[VisualSettingEnum.Normal].GradientColor1;
            }
            set
            {
                m_GradColHdrSettings[VisualSettingEnum.Normal].GradientColor1 = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// The ending color in the gradient path to be used for drawing the gradients of the column headers.
        /// </summary>        
        [Browsable(true), Category("CoolColHeader"),
            Description("The ending color in the gradient path to be used for drawing the gradients of the column headers.")]
        public virtual Color ColHeaderGradientColor2
        {
            get
            {
                return m_GradColHdrSettings[VisualSettingEnum.Normal].GradientColor2;
            }
            set
            {
                m_GradColHdrSettings[VisualSettingEnum.Normal].GradientColor2 = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// Indicates the style/pattern to be used for drawing the gradient backgrounds of the column headers.
        /// </summary>
        [Browsable(true), DefaultValue(CoolGradientType.Horizontal), Category("CoolColHeader"),
         Description("Indicates the style/pattern to be used for drawing the gradient backgrounds of the column headers.")]
        public virtual CoolGradientType ColHeaderGradientType
        {
            get
            {
                return m_GradColHdrSettings[VisualSettingEnum.Normal].GradientType;
            }
            set
            {
                m_GradColHdrSettings[VisualSettingEnum.Normal].GradientType = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// The span (size factor or expanse) of the gradient image to be used for drawing the gradient backgrounds of the column headers.
        /// Certain types of gradients will look more appealing in a control when drawn on a larger or smaller expanse.  Most gradients will look  
        /// ideal drawn with the default gradient span associated with the gradient type.
        /// </summary>        
        [Browsable(true), Category("CoolColHeader"), DefaultValue(1f),
         Description("The span (size factor or expanse) of the gradient image to be used for drawing the gradient backgrounds of the column headers.")]
        public virtual float ColHeaderGradientSpan
        {
            get
            {
                return m_GradColHdrSettings[VisualSettingEnum.Normal].GradientSpan;
            }
            set
            {
                m_GradColHdrSettings[VisualSettingEnum.Normal].GradientSpan = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// Indicates if the default gradient span (size factor or expanse) associated with the selected gradient type used for drawing the gradient 
        /// background of the column headers.  Every gradient type has a default gradient span setting that usually will look most ideal for
        /// the type of gradient being drawn.  When this flag is set to true, the gradient span setting cannot be modified in the control, as the default 
        /// value will be used.
        /// </summary>        
        [Browsable(true), Category("CoolColHeader"), DefaultValue(true),
         Description("Indicates if the default gradient span (size factor or expanse) associated with the selected gradient type used for drawing the " +
                          "gradient background of the column headers.")]
        public virtual bool UseDefaultColHeaderGradientSpan
        {
            get
            {
                return m_GradColHdrSettings[VisualSettingEnum.Normal].UseDefaultGradientSpan;
            }
            set
            {
                m_GradColHdrSettings[VisualSettingEnum.Normal].UseDefaultGradientSpan = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// The X and Y offset position of the gradient image to be used for drawing the gradient background of the column headers.  Adjusting 
        /// the offset position of the gradient alters the lighting and color range of the gradient image in the control.
        /// </summary>        
        [Browsable(true), Category("CoolColHeader"),
         Description("The X and Y offset position of the gradient image to be used for drawing the gradient background of the column headers.")]
        public virtual Point ColHeaderGradientOffset
        {
            get
            {
                return m_GradColHdrSettings[VisualSettingEnum.Normal].GradientOffset;
            }
            set
            {
                m_GradColHdrSettings[VisualSettingEnum.Normal].GradientOffset = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// The border color which will be used for drawing the borders of column headers with gradient backgrounds.
        /// </summary>        
        [Browsable(true), Category("CoolColHeader"),
         Description("The border color which will be used for drawing the borders of column headers with gradient backgrounds.")]
        public virtual Color ColHeaderGradientBorderColor
        {
            get
            {
                return m_BorderColHdrSettings[VisualSettingEnum.Normal].BorderColor;
            }
            set
            {
                m_BorderColHdrSettings[VisualSettingEnum.Normal].BorderColor = value;
                this.Refresh();
            }
        }

        //NOTE: Custom drawing border widths of column headers in the grid is a complicated task and borders will be drawn based on pre-set 
        //style settings, such as those used natively by the grid in future versions.  The user will not have access to the GradientBorderWidth property of 
        //column headers in the grid. 
        /// <summary>
        /// The width of the borders to be drawn around column headers with gradient backgrounds.
        /// </summary>        
        [Browsable(false), DefaultValue(1), Category("CoolColHeader"),
         Description("The width of the borders to be drawn around column headers with gradient backgrounds.")]
        protected virtual int ColHeaderGradientBorderWidth
        {
            get
            {
                return m_BorderColHdrSettings[VisualSettingEnum.Normal].BorderWidth;
            }
            set
            {
                m_BorderColHdrSettings[VisualSettingEnum.Normal].BorderWidth = value;
                this.Refresh();
            }
        }

        #endregion

        #region Column Header Style Related Properties, Functions

        /// <summary>
        /// The font to use for drawing text in the column headers.  Due to a bug in the DataGridView control when custom painting 
        /// is invoked, it will be neccessary to use the ColHeadersFont function to serialize font settings. 
        /// </summary>
        [Browsable(true), Category("CoolColHeader"),
         Description("The font to use for drawing text in the column headers.")]
        public Font ColHeadersFont
        {
            get
            {
                return m_ColHdrFont;
            }
            set
            {
                m_ColHdrFont = value;

                foreach (DataGridViewColumn col in this.Columns)
                {
                    col.HeaderCell.Style.Font = value;
                }//next col

                base.ColumnHeadersDefaultCellStyle.Font = value;

                this.Refresh();
            }
        }

        #endregion

        #region Column Header Translucency Properties, Functions

        /// <summary>
        /// Indicates if the column headers will be drawn with a translucent (semi-transparent) color over the background of the CoolGrid.
        /// </summary>
        [Browsable(true), DefaultValue(false), Category("CoolColHeader"),
         Description("Indicates if the column headers will be drawn with a translucent (semi-transparent) color over the background of the CoolGrid.")]
        public bool DrawColHeaderTransColor
        {
            get
            {
                return m_blDrawColHdrTransColor;
            }
            set
            {
                m_blDrawColHdrTransColor = value;

                if (m_blDrawColHdrTransColor && DrawColHeaderGradient)
                    DrawColHeaderGradient = false;

                this.Refresh();
            }
        }

        /// <summary>
        /// The translucent (semi-transparent) color to be used for drawing the backgrounds of column headers.  
        /// </summary>
        [Browsable(true), Category("CoolColHeader"),
         Description("The translucent (semi-transparent) color to be used for drawing the backgrounds of column headers.")]
        public Color ColHeaderTransColor
        {
            get
            {
                return m_ColHdrTransColor;
            }
            set
            {
                m_ColHdrTransColor = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// The alpha value to be used for drawing translucent (semi-transparent) column headers in the CoolGrid.  
        /// Values range from 0 (transparent) to 255 (opaque).
        /// </summary>
        [Browsable(true), DefaultValue(255), Category("CoolColHeader"),
         Description("The alpha value to be used for drawing translucent (semi-transparent) column headers in the CoolGrid.  " +
                           "Values range from 0 (transparent) to 255 (opaque).")]
        public int ColHeaderTransAlpha
        {
            get
            {
                return m_iColHdrTransAlpha;
            }
            set
            {
                m_iColHdrTransAlpha = value;
                this.Refresh();
            }
        }

        #endregion

        #region Cell Translucency Properties, Functions

        /// <summary>
        /// Indicates if the cells will be drawn with a translucent (semi-transparent) color over the background of the CoolGrid.
        /// </summary>
        [Browsable(true), DefaultValue(false), Category("CoolCell"),
         Description("Indicates if the cells will be drawn with a translucent (semi-transparent) color over the background of the CoolGrid.")]
        public bool DrawCellTransColor
        {
            get
            {
                return m_blDrawCellTransColor;
            }
            set
            {
                m_blDrawCellTransColor = value;

                /* NOT USED: OnCellPainting will now handle all translucent cell painting, no base class painting
                if (value)
                    base.DefaultCellStyle.BackColor = Color.Transparent;
                else
                    base.DefaultCellStyle.BackColor = m_DefaultCellStyle.BackColor;
                */

                this.Refresh();
            }
        }

        /// <summary>
        /// The translucent (semi-transparent) color to be used for drawing the backgrounds of cells in the grid.
        /// </summary>
        [Browsable(true), Category("CoolCell"),
         Description("The translucent (semi-transparent) color to be used for drawing the backgrounds of cells in the grid.")]
        public Color CellTransColor
        {
            get
            {
                return m_CellTransColor;
            }
            set
            {
                m_CellTransColor = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// The alpha value to be used for drawing translucent (semi-transparent) cells in the CoolGrid.  
        /// Values range from 0 (transparent) to 255 (opaque).
        /// </summary>
        [Browsable(true), DefaultValue(255), Category("CoolCell"),
         Description("The alpha value to be used for drawing translucent (semi-transparent) cells in the CoolGrid.  " +
                           "Values range from 0 (transparent) to 255 (opaque).")]
        public int CellTransAlpha
        {
            get
            {
                return m_iCellTransAlpha;
            }
            set
            {
                m_iCellTransAlpha = value;
                this.Refresh();
            }
        }

        #endregion

        #region Selected Cell Translucency Properties, Functions

        /// <summary>
        /// Indicates if the selected cells will be drawn with a translucent (semi-transparent) color over the background of the CoolGrid.
        /// </summary>
        [Browsable(true), DefaultValue(false), Category("CoolCell"),
         Description("Indicates if the selected cells will be drawn with a translucent (semi-transparent) color over the background of the CoolGrid.")]
        public bool DrawSelectedCellTransColor
        {
            get
            {
                return m_blDrawCellSelectTransColor;
            }
            set
            {
                m_blDrawCellSelectTransColor = value;

                this.Refresh();
            }
        }

        /// <summary>
        /// The translucent (semi-transparent) color to be used for drawing the backgrounds of selected cells in the grid.
        /// </summary>
        [Browsable(true), Category("CoolCell"),
         Description("The translucent (semi-transparent) color to be used for drawing the backgrounds of selected cells in the grid.")]
        public Color SelectedCellTransColor
        {
            get
            {
                return m_CellSelectTransColor;
            }
            set
            {
                m_CellSelectTransColor = value;
                this.Refresh();
            }
        }

        /// <summary>
        /// The alpha value to be used for drawing translucent (semi-transparent) selected cells in the CoolGrid.  
        /// Values range from 0 (transparent) to 255 (opaque).
        /// </summary>
        [Browsable(true), DefaultValue(255), Category("CoolCell"),
         Description("The alpha value to be used for drawing translucent (semi-transparent) selected cells in the CoolGrid.  " +
                           "Values range from 0 (transparent) to 255 (opaque).")]
        public int SelectedCellTransAlpha
        {
            get
            {
                return m_iCellSelectTransAlpha;
            }
            set
            {
                m_iCellSelectTransAlpha = value;
                this.Refresh();
            }
        }

        #endregion        
    }
}
