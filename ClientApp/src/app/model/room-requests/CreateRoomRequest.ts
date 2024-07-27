export class CreateRoomRequest {
    constructor(public roomName: string, public password: string, public encryptedSymmetricRoomKey: string) { }
}