package sfu

import (
	"voice-sfu/server/domain/store"
	"fmt"

	"github.com/pion/webrtc/v2"
)

func Publish(dc *webrtc.DataChannel, room string, uu string) {
	dc.OnMessage(func(msg webrtc.DataChannelMessage) {
		fmt.Println("publish", string(msg.Data))
		groupe := store.GetDatachannels(room)
		for k, v := range groupe {
			if k != uu {
				if msg.IsString == true {
					v.SendText(string(msg.Data))
				} else {
					v.Send(msg.Data)
				}

			}
		}
	})
}
