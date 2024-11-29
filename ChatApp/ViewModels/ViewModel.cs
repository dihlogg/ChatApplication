using ChatApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Windows.Input;
using ChatApp.Commands;
using System.Windows;
using System.Windows.Media;

namespace ChatApp.ViewModels
{
    public class ViewModel : INotifyPropertyChanged
    {
        // init resource dictionary file
        private readonly ResourceDictionary dictionary = Application.LoadComponent(new Uri("/ChatApp;component/Assets/icons.xaml", UriKind.RelativeOrAbsolute)) as
         ResourceDictionary;
        public ObservableCollection<StatusDataModel> statusThumbsCollection {  get; set; }
        public ObservableCollection<ChatListData> mChats;
        public ObservableCollection<ChatListData> mPinnedChats;
        protected ObservableCollection<ChatListData> _archivedChats;
        public ObservableCollection<ChatListData> ArchivedChats
        {
            get => _archivedChats;
            set
            {
                _archivedChats = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<ChatListData> Chats
        {
            get => mChats;
            set
            {
                // to change the list
                if (mChats == value)
                    return;

                // to update the list
                mChats = value;

                // Updating filtered chats to match
                FilteredChats = new ObservableCollection<ChatListData>(mChats);
                OnPropertyChanged("Chats");
                OnPropertyChanged("FilteredChats");
            }
        }

        public ObservableCollection<ChatListData> PinnedChats
        {
            get => mPinnedChats;
            set
            {
                // to change the list
                if (mPinnedChats == value)
                    return;

                // to update the list
                mPinnedChats = value;

                // Updating filtered chats to match
                FilteredPinnedChats = new ObservableCollection<ChatListData>(mPinnedChats);
                OnPropertyChanged("PinnedChats");
                OnPropertyChanged("FilteredPinnedChats");
            }
        }
        // Filtering & Pinned chat
        public ObservableCollection<ChatListData> FilteredChats { get; set; }
        public ObservableCollection<ChatListData> FilteredPinnedChats
        {
            get => _filteredPinnedChats;
            set
            {
                _filteredPinnedChats = value;
                OnPropertyChanged();
            }
        }
        private ObservableCollection<ChatListData> _filteredPinnedChats;

        protected ObservableCollection<ChatConversation> mConversations;
        public ObservableCollection<ChatConversation> Conversations {
            get => mConversations;
            set
            {
                // to change the list
                if (mConversations == value)
                    return;

                // to update the list
                mConversations = value;

                // Updating filtered chats to match
                FilteredConversations = new ObservableCollection<ChatConversation>(mConversations);
                OnPropertyChanged("Conversations");
                OnPropertyChanged("FilteredConversations");
            }
        }
        // Container the window options
        public ObservableCollection<MoreOptionsMenu> _windowMoreOptionsMenuList;
        public ObservableCollection<MoreOptionsMenu> WindowMoreOptionsMenuList
        {
            get
            {
                return _windowMoreOptionsMenuList;
            }
            set
            {
                _windowMoreOptionsMenuList = value;
            }
        }

        // Container the Attachment Menu
        public ObservableCollection<MoreOptionsMenu> _attachmentOptionsMenuList;
        public ObservableCollection<MoreOptionsMenu> AttachmentOptionsMenuList
        {
            get
            {
                return _attachmentOptionsMenuList;
            }
            set
            {
                _attachmentOptionsMenuList = value;
            }
        }
        protected void UpdateChatAndMoveUp(ObservableCollection<ChatListData> chatList, ChatConversation conversation)
        {
            //if message sent is to the selected contact or not
            var chat = chatList.FirstOrDefault(x => x.ContactName == ContactName);
            if(chat != null)
            {
                // update contact chat last message
                chat.Message = MessageText;
                chat.LastMessageTime = conversation.MsgSentOn;

                // move chat on top when new message is received/sent
                chatList.Move(chatList.IndexOf(chat), 0);

                // update collection
                OnPropertyChanged("Chats");
                OnPropertyChanged("PinnedChats");
                OnPropertyChanged("FilteredChats");
                OnPropertyChanged("FilteredPinnedChats");
                OnPropertyChanged("ArchivedChats");
            }
        }

        // Contact Info
        protected bool _IsContactInfoOpen;
        public bool IsContactInfoOpen
        {
            get => _IsContactInfoOpen;
            set
            {
                _IsContactInfoOpen = value;
                OnPropertyChanged("IsContactInfoOpen");
            }
        }

        // Use this message text to transfer the send message value
        protected string messageText;
        public string MessageText
        {
            get => messageText;
            set
            {
                messageText = value;
                OnPropertyChanged("messageText");
            }
        }
        protected string LastSearchConversationText;
        protected string mSearchConversationText;
        public string SearchConversationText
        {
            get => mSearchConversationText;
            set
            {
                // checked if value is different
                if (mSearchConversationText == value)
                    return;
                // update value
                mSearchConversationText = value;
                //if search text is empty restore messages
                if (string.IsNullOrEmpty(SearchConversationText))
                    SearchInConversation();
            }
        }
        public string MessageToReplyText { get; set; }
        public bool FocusMessageBox {  get; set; }
        public bool IsThisAReplyMessage { get; set; }
        public ObservableCollection<ChatConversation> FilteredConversations { get; set; }
        protected ICommand _getSelectedChatCommand;
        public ICommand GetSelectedChatCommand => _getSelectedChatCommand ??= new RelayCommand(parameter =>
        {
            if(parameter is ChatListData v)
            {
                // getting contact name from selected chat
                ContactName = v.ContactName;
                OnPropertyChanged("ContactName");

                // getting contact photo from selected chat
                ContactPhoto = v.ContactPhoto;
                OnPropertyChanged("ContactPhoto");

                LoadChatConversation(v);
            }
        });
        protected ICommand _replyCommand;
        public ICommand ReplyCommand => _replyCommand ??= new RelayCommand(parameter =>
        {
            if (parameter is ChatConversation v)
            {
                // if reply sender message
                if (v.IsMessageReceived)
                    MessageToReplyText = v.ReceivedMessage;
                // if reply own message
                else
                    MessageToReplyText = v.SentMessage;
                // update
                OnPropertyChanged("MessageToReplyText");

                // set focus on messgae box when user click reply button
                FocusMessageBox = true;
                OnPropertyChanged("FocusMessageBox");

                // flag this message as reply message
                IsThisAReplyMessage = true;
                OnPropertyChanged("IsThisAReplyMessage");
            }
        });
        protected ICommand _cancelReplyCommand;
        public ICommand CancelReplyCommand
        {
            get
            {
                if (_cancelReplyCommand == null)
                    _cancelReplyCommand = new CommandViewModel(CancelReply);
                return _cancelReplyCommand;
            }
            set
            {
                _cancelReplyCommand = value;
            }
        }
        // to pin chat on pin button click
        protected ICommand _pinChatCommand;
        public ICommand PinChatCommand => _pinChatCommand ??= new RelayCommand(parameter =>
        {
            if (parameter is ChatListData v)
            {
                if (!FilteredPinnedChats.Contains(v))
                {
                    PinnedChats.Add(v);
                    FilteredPinnedChats.Add(v);
                    OnPropertyChanged("PinnedChats");
                    OnPropertyChanged("FilteredPinnedChats");
                    v.ChatIsPinned = true;

                    Chats.Remove(v);
                    FilteredChats.Remove(v);
                    OnPropertyChanged("Chats");
                    OnPropertyChanged("FilteredChats");

                    if(ArchivedChats != null)
                    {
                        if(ArchivedChats.Contains(v))
                        {
                            ArchivedChats.Remove(v);
                            v.ChatIsArchived = false;
                        }
                    }

                    //OnPropertyChanged(nameof(FilteredPinnedChats));
                    //OnPropertyChanged(nameof(PinnedChats));
                }
            }
        });

        // to unpin chat
        protected ICommand _unPinChatCommand;
        public ICommand UnPinChatCommand => _unPinChatCommand ??= new RelayCommand(parameter =>
        {
            if (parameter is ChatListData v)
            {
                if (!FilteredChats.Contains(v))
                {
                    // add select chat to normal unpinned chat list
                    Chats.Add(v);
                    FilteredChats.Add(v);
                    OnPropertyChanged("Chats");
                    OnPropertyChanged("FilteredChats");

                    // remove selected pinned chats list
                    PinnedChats.Remove(v);
                    FilteredPinnedChats.Remove(v);
                    OnPropertyChanged("PinnedChats");
                    OnPropertyChanged("FilteredPinnedChats");
                    v.ChatIsPinned = false;
                }
            }
        });

        // archive chat command
        protected ICommand _archiveChatCommand;
        public ICommand ArchiveChatCommand => _archiveChatCommand ??= new RelayCommand(parameter =>
        {
            if (parameter is ChatListData v)
            {
                if (!ArchivedChats.Contains(v))
                {
                    // add chat in archive list
                    ArchivedChats.Add(v);
                    v.ChatIsArchived = true;
                    v.ChatIsPinned = false;

                    // remove chat fr chat lisst
                    Chats.Remove(v);
                    FilteredChats.Remove(v);
                    PinnedChats.Remove(v);
                    FilteredPinnedChats.Remove(v);

                    // update list
                    OnPropertyChanged("Chats");
                    OnPropertyChanged("FilteredChats");
                    OnPropertyChanged("PinnedChats");
                    OnPropertyChanged("FilteredPinnedChats");
                    OnPropertyChanged("ArchivedChats");
                }
            }
        });

        // unarchive chat command
        protected ICommand _unArchiveChatCommand;
        public ICommand UnArchiveChatCommand => _unArchiveChatCommand ??= new RelayCommand(parameter =>
        {
            if (parameter is ChatListData v)
            {
                if (!FilteredChats.Contains(v) && !Chats.Contains(v))
                {
                    Chats.Add(v);
                    FilteredChats.Add(v);

                    // remove chat in archive list
                    ArchivedChats.Remove(v);
                    v.ChatIsArchived = false;
                    v.ChatIsPinned = false;

                    OnPropertyChanged("ArchivedChats");
                    OnPropertyChanged("Chats");
                    OnPropertyChanged("FilteredChats");
                }
            }
        });
        protected ICommand _windowMoreOptionsCommand;
        public ICommand WindowMoreOptionsCommand
        {
            get
            {
                if (_windowMoreOptionsCommand == null)
                    _windowMoreOptionsCommand = new CommandViewModel(WindowMoreOptionsMenu);
                return _windowMoreOptionsCommand;
            }
            set
            {
                _windowMoreOptionsCommand = value;
            }
        }
        protected ICommand _conversationScreenMoreOptionsCommand;
        public ICommand ConversationScreenMoreOptionsCommand
        {
            get
            {
                if (_conversationScreenMoreOptionsCommand == null)
                    _conversationScreenMoreOptionsCommand = new CommandViewModel(ConversationScreenMoreOptionsMenu);
                return _conversationScreenMoreOptionsCommand;
            }
            set
            {
                _conversationScreenMoreOptionsCommand = value;
            }
        }
        protected ICommand _attachmentOptionsCommand;
        public ICommand AttachmentOptionsCommand
        {
            get
            {
                if (_attachmentOptionsCommand == null)
                    _attachmentOptionsCommand = new CommandViewModel(AttachmentOptionsMenu);
                return _attachmentOptionsCommand;
            }
            set
            {
                _attachmentOptionsCommand = value;
            }
        }

        protected ICommand _openSearchCommand;
        public ICommand OpenSearchCommand
        {
            get
            {
                if (_openSearchCommand == null)
                    _openSearchCommand = new CommandViewModel(OpenSearchBox);
                return _openSearchCommand;
            }
            set
            {
                _openSearchCommand = value;
            }
        }
        protected ICommand _closeSearchCommand;
        public ICommand CloseSearchCommand
        {
            get
            {
                if (_closeSearchCommand == null)
                    _closeSearchCommand = new CommandViewModel(CloseSearchBox);
                return _closeSearchCommand;
            }
            set
            {
                _closeSearchCommand = value;
            }
        }
        protected ICommand _clearSearchCommand;
        public ICommand ClearSearchCommand
        {
            get
            {
                if (_clearSearchCommand == null)
                    _clearSearchCommand = new CommandViewModel(ClearSearchBox);
                return _clearSearchCommand;
            }
            set
            {
                _clearSearchCommand = value;
            }
        }
        protected ICommand _searchCommand;
        public ICommand SearchCommand
        {
            get
            {
                if (_searchCommand == null)
                    _searchCommand = new CommandViewModel(Search);
                return _searchCommand;
            }
            set
            {
                _searchCommand = value;
            }
        }
        protected ICommand _searchConversationCommand;
        public ICommand SearchConversationCommand
        {
            get
            {
                if (_searchConversationCommand == null)
                    _searchConversationCommand = new CommandViewModel(SearchInConversation);
                return _searchConversationCommand;
            }
            set
            {
                _searchConversationCommand = value;
            }
        }
        protected ICommand _openConversationSearchCommand;
        public ICommand OpenConversationSearchCommand
        {
            get
            {
                if (_openConversationSearchCommand == null)
                    _openConversationSearchCommand = new CommandViewModel(OpenConversationSearchBox);
                return _openConversationSearchCommand;
            }
            set
            {
                _openConversationSearchCommand = value;
            }
        }
        protected ICommand _closeConversationSearchCommand;
        public ICommand CloseConversationSearchCommand
        {
            get
            {
                if (_closeConversationSearchCommand == null)
                    _closeConversationSearchCommand = new CommandViewModel(CloseConversationSearchBox);
                return _closeConversationSearchCommand;
            }
            set
            {
                _closeConversationSearchCommand = value;
            }
        }
        protected ICommand _clearConversationSearchCommand;
        public ICommand ClearConversationSearchCommand
        {
            get
            {
                if (_clearConversationSearchCommand == null)
                    _clearConversationSearchCommand = new CommandViewModel(ClearConversationSearchBox);
                return _clearConversationSearchCommand;
            }
            set
            {
                _clearConversationSearchCommand = value;
            }
        }
        protected ICommand _openContactInfoCommand;
        public ICommand OpenContactInfoCommand
        {
            get
            {
                if (_openContactInfoCommand == null)
                    _openContactInfoCommand = new CommandViewModel(OpenContactInfo);
                return _openContactInfoCommand;
            }
            set
            {
                _openContactInfoCommand = value;
            }
        }
        protected ICommand _closeContactInfoCommand;
        public ICommand CloseContactInfoCommand
        {
            get
            {
                if (_closeContactInfoCommand == null)
                    _closeContactInfoCommand = new CommandViewModel(CloseContactInfo);
                return _closeContactInfoCommand;
            }
            set
            {
                _closeContactInfoCommand = value;
            }
        }
        public string ContactName { get; set; }
        public byte[] ContactPhoto {  get; set; }
        public string LastSeen { get; set; }
        protected bool _isSearchBoxOpen;
        public bool IsSearchBoxOpen
        {
            get => _isSearchBoxOpen;
            set
            {
                if (_isSearchBoxOpen == value)
                    return;

                _isSearchBoxOpen = value;

                if (_isSearchBoxOpen == value)
                    // clear search box
                    SearchText = string.Empty;
                OnPropertyChanged("IsSearchBoxOpen");
                OnPropertyChanged("SearchText");
            }
        }
        protected bool _isConversationSearchBoxOpen;
        public bool IsConversationSearchBoxOpen
        {
            get => _isConversationSearchBoxOpen;
            set
            {
                if (_isConversationSearchBoxOpen == value)
                    return;

                _isConversationSearchBoxOpen = value;

                if (_isConversationSearchBoxOpen == value)
                    // clear search box
                    SearchConversationText = string.Empty;
                OnPropertyChanged("IsConversationSearchBoxOpen");
                OnPropertyChanged("SearchConversationText");
            }
        }
        protected bool _isSearchConversationBoxOpen;
        public bool IsSearchConversationBoxOpen
        {
            get => _isSearchConversationBoxOpen;
            set
            {
                if (_isSearchConversationBoxOpen == value)
                    return;

                _isSearchConversationBoxOpen = value;

                if (_isSearchConversationBoxOpen == value)
                    // clear search box
                    SearchConversationText = string.Empty;
                OnPropertyChanged("IsSearchConversationBoxOpen");
                OnPropertyChanged("SearchConversationText");
            }
        }
        protected string LastSearchText { get; set; }
        protected string mSearchText { get; set; }
        public string SearchText
        {
            get => mSearchText;
            set
            {
                // checked if value is different
                if (mSearchText == value)
                    return;
                // update value
                mSearchText = value;
                //if search text is empty restore messages
                if (string.IsNullOrEmpty(SearchText))
                    Search();
            }
        }
        protected ICommand _sendMessageCommand;
        public ICommand SendMessageCommand
        {
            get
            {
                if (_sendMessageCommand == null)
                    _sendMessageCommand = new CommandViewModel(SendMessage);
                return _sendMessageCommand;
            }
            set
            {
                _sendMessageCommand = value;
            }
        }
        string connectionString = "Data Source=ROMBEO;Initial Catalog=ChatApp;User ID=sa;Password=123456;Encrypt=False";

        void LoadStatusThumbs()
        {
            statusThumbsCollection = new ObservableCollection<StatusDataModel>()
            {
                // Keep first status blank for the user to add own status
                new() {
                    IsMeAddStatus = true
                },
                new() {
                    ContactName = "Long",
                    ContactPhoto = new Uri("pack://application:,,,/Assets/3.jpg", UriKind.Absolute),
StatusImage = new Uri("pack://application:,,,/Assets/8.jpg", UriKind.Absolute),
                    IsMeAddStatus = false
                },
                new() {
                    ContactName = "Huy",
                    ContactPhoto = new Uri("/Assets/3.jpg", UriKind.RelativeOrAbsolute),
                    StatusImage = new Uri("/Assets/8.jpg", UriKind.RelativeOrAbsolute),
                    IsMeAddStatus = false
                },new() {
                    ContactName = "Sang",
                    ContactPhoto = new Uri("/Assets/4.jpg", UriKind.RelativeOrAbsolute),
                    StatusImage = new Uri("/Assets/6.jpg", UriKind.RelativeOrAbsolute),
                    IsMeAddStatus = false
                },new() {
                    ContactName = "Na",
                    ContactPhoto = new Uri("/Assets/4.jpg", UriKind.RelativeOrAbsolute),
                    StatusImage = new Uri("/Assets/6.jpg", UriKind.RelativeOrAbsolute),
                    IsMeAddStatus = false
                },
            };
            OnPropertyChanged("statusThumbsCollection");
        }
        void LoadChats()
        {
            //loading chat from db
            if (Chats == null)
                Chats = new ObservableCollection<ChatListData>();

            //open sql connection
            connection.Open();

            //temporary collection
            ObservableCollection<ChatListData> temp = new ObservableCollection<ChatListData>();
            using (SqlCommand command = new SqlCommand("select * from contacts p left join (select a.*, row_number() over(partition by a.contactname order by a.id desc) as seqnum from conversations a ) a on a.ContactName = p.contactname and a.seqnum = 1 order by a.Id desc", connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    // to avoid duplicate
                    string lastItem = string.Empty;
                    string newItem = string.Empty;

                    while (reader.Read())
                    {
                        string time = string.Empty;
                        string lastMessage = string.Empty;
                        if (!string.IsNullOrEmpty(reader["MsgReceivedOn"].ToString()))
                        {
                            time = Convert.ToDateTime(reader["MsgReceivedOn"].ToString()).ToString("ddd hh:mm tt");
                            lastMessage = reader["ReceivedMsgs"].ToString();
                        }
                        if (!string.IsNullOrEmpty(reader["MsgSentOn"].ToString()))
                        {
                            time = Convert.ToDateTime(reader["MsgSentOn"].ToString()).ToString("ddd hh:mm tt");
                            lastMessage = reader["SentMsgs"].ToString();
                        }

                        // if the chat is new or stared new conversation show start new conversation
                        if(string.IsNullOrEmpty(lastMessage))
                        lastMessage = "Start New Conversation";

                        //update data in model
                        ChatListData chat = new ChatListData()
                        {
                            ContactPhoto = (byte[])reader["ContactPhoto"],
                            ContactName = reader["contactname"].ToString(),
                            Message = lastMessage,
                            LastMessageTime = time
                        };

                        //update
                        newItem = reader["contactname"].ToString();
                        if(lastItem != newItem)
                            temp.Add(chat);

                        lastItem = newItem;
                    }
                }
            }
            //Tranfer data
            Chats = temp;
            //Update
            OnPropertyChanged("Chats");
        }
        void LoadChatConversation(ChatListData chat)
        {
            if (chat == null)
            {
                throw new ArgumentNullException(nameof(chat), "Chat object cannot be null.");
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    if (Conversations == null)
                    {
                        Conversations = new ObservableCollection<ChatConversation>();
                    }
                    Conversations.Clear();
                    FilteredConversations.Clear();

                    using (SqlCommand com = new SqlCommand("SELECT * FROM Conversations WHERE LTRIM(RTRIM(LOWER(ContactName))) = LTRIM(RTRIM(LOWER(@ContactName)))", connection))
                    {
                        com.Parameters.AddWithValue("@ContactName", chat.ContactName ?? string.Empty);

                        using (SqlDataReader reader = com.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                Console.WriteLine($"Không có dữ liệu cho {chat.ContactName}");
                                return;
                            }

                            while (reader.Read())
                            {
                                var conversation = new ChatConversation()
                                {
                                    ContactName = reader["ContactName"]?.ToString() ?? string.Empty,
                                    ReceivedMessage = reader["ReceivedMsgs"]?.ToString() ?? string.Empty,
                                    MsgReceivedOn = !reader.IsDBNull(reader.GetOrdinal("MsgReceivedOn"))
                                        ? Convert.ToDateTime(reader["MsgReceivedOn"]).ToString("MMM dd, hh:mm tt")
                                        : "",
                                    SentMessage = reader["SentMsgs"]?.ToString() ?? string.Empty,
                                    MsgSentOn = !reader.IsDBNull(reader.GetOrdinal("MsgSentOn"))
                                        ? Convert.ToDateTime(reader["MsgSentOn"]).ToString("MMM dd, hh:mm tt")
                                        : "",
                                    IsMessageReceived = !string.IsNullOrEmpty(reader["ReceivedMsgs"]?.ToString())
                                };

                                Conversations.Add(conversation);
                                OnPropertyChanged("Conversations");
                                FilteredConversations.Add(conversation);
                                OnPropertyChanged("FilteredConversations");

                                chat.Message = !string.IsNullOrEmpty(reader["ReceivedMsgs"].ToString()) ? reader["ReceivedMsgs"].ToString() :
                                    reader["SentMsgs"].ToString();
                            }
                        }
                    }
                    // reset reply message text when the new is fetched
                    MessageToReplyText = string.Empty;
                    OnPropertyChanged("MessageToReplyText");
                }

                // Cập nhật giao diện sau khi hoàn tất
                OnPropertyChanged("Conversations");
                Console.WriteLine($"Đã tải {Conversations.Count} đoạn hội thoại cho {chat.ContactName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tải đoạn hội thoại: {ex.Message}");
            }
        }
        void SearchInConversation()
        {
            // to avoid re search same text again
            if ((string.IsNullOrEmpty(LastSearchConversationText) && string.IsNullOrEmpty(SearchConversationText)) || string.Equals(LastSearchConversationText, SearchConversationText))
                return;
            // if searchbox is empty
            if (string.IsNullOrEmpty(SearchConversationText) || Chats == null || Chats.Count <= 0)
            {
                FilteredConversations = new ObservableCollection<ChatConversation>(Conversations ?? Enumerable.Empty<ChatConversation>());
                OnPropertyChanged("FilteredConversations");

                // update last search text
                LastSearchConversationText = SearchConversationText;
                return;
            }
            FilteredConversations = new ObservableCollection<ChatConversation>(
                Conversations.Where(chat => chat.ReceivedMessage.ToLower().Contains(SearchConversationText) || chat.SentMessage.ToLower().Contains(SearchConversationText)));
            OnPropertyChanged("FilteredConversations");

            LastSearchConversationText = SearchConversationText;
        }
        void WindowMoreOptionsMenu()
        {
            WindowMoreOptionsMenuList = new ObservableCollection<MoreOptionsMenu>()
            {
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["newgroup"],
                    MenuText = "New Group"
                },
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["newbroadcast"],
                    MenuText = "New Broadcast"
                },
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["starredmessages"],
                    MenuText = "Starred Messages"
                },
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["settings"],
                    MenuText = "Settings"
                },
            };
            OnPropertyChanged("WindowMoreOptionsMenuList");
        }
        void ConversationScreenMoreOptionsMenu()
        {
            WindowMoreOptionsMenuList = new ObservableCollection<MoreOptionsMenu>()
            {
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["alllmedia"],
                    MenuText = "All Media"
                },
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["wallpaper"],
                    MenuText = "Change Wallpaper"
                },
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["report"],
                    MenuText = "Report"
                },
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["block"],
                    MenuText = "Block"
                },
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["clearchat"],
                    MenuText = "Clear Chat"
                },
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["exportchat"],
                    MenuText = "Export Chat"
                },
            };
            OnPropertyChanged("WindowMoreOptionsMenuList");
        }
        void AttachmentOptionsMenu()
        {
            AttachmentOptionsMenuList = new ObservableCollection<MoreOptionsMenu>()
            {
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["docs"],
                    MenuText = "Docs",
                    BorderStroke = "#3F3990",
                    Fill = "#CFCEEC"
                },
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["camera"],
                    MenuText = "Camera",
                    BorderStroke = "#2C5A71",
                    Fill = "#C5E7F8"
                },
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["gallery"],
                    MenuText = "Gallery",
                    BorderStroke = "#EA2140",
                    Fill = "#F3BEBE"
                },
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["audio"],
                    MenuText = "Audio",
                    BorderStroke = "#E67E00",
                    Fill = "#F7D5AC"
                },
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["location"],
                    MenuText = "Location",
                    BorderStroke = "#28C58F",
                    Fill = "#E3F5EF"
                },
                new MoreOptionsMenu()
                {
                    Icon = (PathGeometry)dictionary["contact"],
                    MenuText = "Contact",
                    BorderStroke = "#0093E0",
                    Fill = "#DDF1FB"
                },
            };
            OnPropertyChanged("AttachmentOptionsMenuList");
        }
        public void CancelReply()
        {
            IsThisAReplyMessage = false;
            // reset reply message text
            MessageToReplyText = string.Empty;
            OnPropertyChanged("MessageToReplyText");
        }
        public void SendMessage()
        {
            // send message only when the text box not empty
            if(!string.IsNullOrEmpty(MessageText))
            {
                var conversation = new ChatConversation()
                {
                    ReceivedMessage = MessageToReplyText,
                    SentMessage = MessageText,
                    MsgSentOn = DateTime.Now.ToString("MMM dd, hh:mm tt"),
                    MessageContainsReply = IsThisAReplyMessage
                };
                // add message to conversation list
                FilteredConversations.Add(conversation);
                Conversations.Add(conversation);

                // clear message prop and text box when message is sent
                MessageText = string.Empty;
                IsThisAReplyMessage = false;
                MessageToReplyText = string.Empty;

                UpdateChatAndMoveUp(Chats, conversation);
                UpdateChatAndMoveUp(PinnedChats, conversation);
                UpdateChatAndMoveUp(FilteredChats, conversation);
                UpdateChatAndMoveUp(FilteredPinnedChats, conversation);
                UpdateChatAndMoveUp(ArchivedChats, conversation);

                // update
                OnPropertyChanged("FilteredConversations");
                OnPropertyChanged("Conversations");
                OnPropertyChanged("MessageText");
                OnPropertyChanged("IsThisAReplyMessage");
                OnPropertyChanged("MessageToReplyText");
            }
        }
        string FormatDateTime(object dateTimeValue)
        {
            if (dateTimeValue == null || dateTimeValue == DBNull.Value)
                return string.Empty;

            return Convert.ToDateTime(dateTimeValue).ToString("MMM dd, hh:mm tt");
        }

        public void Search()
        {
            // to avoid re search same text again
            if ((string.IsNullOrEmpty(LastSearchText) && string.IsNullOrEmpty(SearchText)) || string.Equals(LastSearchText, SearchText))
                return;
            // if searchbox is empty
            if (string.IsNullOrEmpty(SearchText) || Chats == null || Chats.Count <= 0)
            {
                FilteredChats = new ObservableCollection<ChatListData>(Chats ?? Enumerable.Empty<ChatListData>());
                OnPropertyChanged("FilteredChats");

                FilteredPinnedChats = new ObservableCollection<ChatListData>(PinnedChats ?? Enumerable.Empty<ChatListData>());
                OnPropertyChanged("FilteredPinnedChats");

                // update last search text
                LastSearchText = SearchText;
                return;
            }
            FilteredChats = new ObservableCollection<ChatListData>(
                Chats.Where(chat => chat.ContactName.Contains(SearchText)
                || chat.Message != null && chat.Message.ToLower().Contains(SearchText)));
            OnPropertyChanged("FilteredChats");

            FilteredPinnedChats = new ObservableCollection<ChatListData>(
                PinnedChats.Where(pinnedchat => pinnedchat.ContactName.Contains(SearchText)
                || pinnedchat.Message != null && pinnedchat.Message.ToLower().Contains(SearchText)));
            OnPropertyChanged("FilteredPinnedChats");

            LastSearchText = SearchText;
        }
        public void OpenSearchBox()
        {
            IsSearchBoxOpen = true;
        }
        public void CloseSearchBox() => IsSearchBoxOpen = false;
        public void ClearSearchBox()
        {
            if (!string.IsNullOrEmpty(SearchText))
                SearchText = string.Empty;
            else
                CloseSearchBox();
        }
        public void OpenConversationSearchBox()
        {
            IsSearchConversationBoxOpen = true;
        }
        public void CloseConversationSearchBox() => IsSearchConversationBoxOpen = false;
        public void ClearConversationSearchBox()
        {
            if (!string.IsNullOrEmpty(SearchConversationText))
                SearchConversationText = string.Empty;
            else
                CloseSearchBox();
        }
        public void OpenContactInfo() => IsContactInfoOpen = true;
        public void CloseContactInfo() => IsContactInfoOpen = false;
        SqlConnection connection = new SqlConnection("Data Source=ROMBEO;Initial Catalog=ChatApp;User ID=sa;Password=123456;Encrypt=False");
        public ViewModel()
        {
            LoadStatusThumbs();
            LoadChats();
            PinnedChats = new ObservableCollection<ChatListData>();
            FilteredPinnedChats = new ObservableCollection<ChatListData>();
            ArchivedChats = new ObservableCollection<ChatListData>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
