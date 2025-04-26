export interface FriendRequest {
    senderId: string;
    receiver: string;
}

export interface FriendRequestManage {
    requestId: string;
    senderName: string;
    senderId: string;
    sentTime: Date;
    receiverName: string;
    receiverId: string;
}

export interface FriendRequestManageRequest {
    requestId: string;
    userId: string;
}

export interface DeleteFriendRequest {
    requestId: string;
    senderId: string;
    receiverId: string;
}

export interface ChatRoomInviteRequest {
    roomId: string;
    roomName: string;
    receiverName: string;
    senderId: string;
    senderName: string;
    roomKey?: string;
}