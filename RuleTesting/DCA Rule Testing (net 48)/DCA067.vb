#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.ComponentModel

#End Region

#Region " DCA067 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA067: String literal instead of NameOf.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA067 (Info) on <c>"Title"</c> passed to the <c>propertyName</c> parameter of <c>OnPropertyChanged</c>: a
    ''' rename of the property does not update the string. This rule runs only in .NET Framework projects (on modern .NET,
    ''' the built-in rule CA1507 reports the same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>NameOf(Title)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA067

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA067"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Settings that notify the changes of their properties.
        ''' </summary>
        Friend NotInheritable Class Settings : Implements INotifyPropertyChanged

            ''' <summary>
            ''' The value of <see cref="Title"/>.
            ''' </summary>
            Private titleValue As String

            ''' <summary>
            ''' Initializes a new instance of the <see cref="Settings"/> class.
            ''' </summary>
            Friend Sub New()
            End Sub

            ''' <summary>
            ''' Occurs when a property value changes.
            ''' </summary>
            Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

            ''' <summary>
            ''' Gets or sets the title.
            ''' </summary>
            Friend Property Title As String
                Get
                    Return Me.titleValue
                End Get
                Set(value As String)
                    Me.titleValue = value
                    Me.OnPropertyChanged("Title") ' DCA067
                End Set
            End Property

            ''' <summary>
            ''' Raises the <see cref="PropertyChanged"/> event.
            ''' </summary>
            '''
            ''' <param name="propertyName">
            ''' The name of the changed property.
            ''' </param>
            Private Sub OnPropertyChanged(propertyName As String)

                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
            End Sub

        End Class

#End Region

    End Class

End Namespace

#End Region
