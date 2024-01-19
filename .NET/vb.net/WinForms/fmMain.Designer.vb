<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class fmMain
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.lbLog = New System.Windows.Forms.ListBox()
        Me.btStop = New System.Windows.Forms.Button()
        Me.btStart = New System.Windows.Forms.Button()
        Me.tiWait = New System.Windows.Forms.Timer(Me.components)
        Me.SuspendLayout()
        '
        'lbLog
        '
        Me.lbLog.FormattingEnabled = True
        Me.lbLog.Location = New System.Drawing.Point(12, 40)
        Me.lbLog.Name = "lbLog"
        Me.lbLog.Size = New System.Drawing.Size(607, 368)
        Me.lbLog.TabIndex = 5
        '
        'btStop
        '
        Me.btStop.Location = New System.Drawing.Point(93, 11)
        Me.btStop.Name = "btStop"
        Me.btStop.Size = New System.Drawing.Size(75, 23)
        Me.btStop.TabIndex = 4
        Me.btStop.Text = "Stop"
        Me.btStop.UseVisualStyleBackColor = True
        '
        'btStart
        '
        Me.btStart.Location = New System.Drawing.Point(12, 11)
        Me.btStart.Name = "btStart"
        Me.btStart.Size = New System.Drawing.Size(75, 23)
        Me.btStart.TabIndex = 3
        Me.btStart.Text = "Start"
        Me.btStart.UseVisualStyleBackColor = True
        '
        'tiWait
        '
        Me.tiWait.Interval = 5000
        '
        'fmMain
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(631, 418)
        Me.Controls.Add(Me.lbLog)
        Me.Controls.Add(Me.btStop)
        Me.Controls.Add(Me.btStart)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.Name = "fmMain"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Few devices pairing demo application"
        Me.ResumeLayout(False)

    End Sub

    Private WithEvents lbLog As ListBox
    Private WithEvents btStop As Button
    Private WithEvents btStart As Button
    Private WithEvents tiWait As Timer
End Class
