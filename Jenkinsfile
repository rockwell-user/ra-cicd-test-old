pipeline {
    agent { 
        node {
            label 'docker-agent-alpine'
            }
      }
    triggers {
        pollSCM '* * * * *'
    }
    stages {
        stage('Build') {
            steps {
                echo "Building.."
                echo "BUILD TEST"
            }
        }
        stage('Test') {
            steps {
                echo "Testing.."
            }
        }
        stage('Publish') {
            steps {
                echo "Publishing.."
            }
        }

        stage('Deliver') {
            steps {
                echo 'Delivering....'
            }
        }
    }
}
